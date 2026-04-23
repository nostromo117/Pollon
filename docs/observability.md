# Osservabilità e Tracciamento Distribuito

Questo documento descrive come Pollon implementa l'osservabilità (Distributed Tracing, Logging e Metrics) per permettere il monitoraggio e il debugging end-to-end delle richieste in un ambiente a microservizi.

## Architettura di Tracciamento: Dual-Export Nativo

Pollon utilizza **OpenTelemetry (OTel)** come standard per la raccolta di telemetria, implementando un'architettura di **Dual-Export Nativo**. Per mantenere stabilità e aggirare le naturali restrizioni di rete di Docker Desktop, ogni microservizio genera e invia la propria telemetria simultaneamente a due destinazioni:

1.  **.NET Aspire Dashboard** (Processo Host): Per la visualizzazione in tempo reale di Log, Metriche e Tracce base, sfruttando l'autenticazione nativa di sistema.
2.  **OpenTelemetry Collector** (Container Docker): Riceve una copia parallela esclusivamente delle tracce. Si occupa di arricchirle (es. identificando RabbitMQ e MinIO come nodi infrastrutturali) e le inoltra a **Jaeger** per le analisi grafiche più avanzate.

Questa architettura parallela e "senza ponti interconnessi" garantisce l'assenza di errori di autorizzazione (`401 Unauthorized`) o irraggiungibilità di rete, massimizzando l'affidabilità dell'osservabilità locale.

### Propagazione del TraceId
Quando una richiesta entra nel sistema (es: tramite il Frontend), viene generato un **TraceId** univoco. Questo ID viene propagato automaticamente attraverso tutti i confini del servizio:
1.  **Chiamata HTTP**: Il `TraceId` viene inserito nell'header standard `traceparent`.
2.  **Messaggistica Asincrona (Wolverine)**: Wolverine include il `TraceId` nei metadati del messaggio RabbitMQ. Quando il consumer nel `Content.Api` riceve il messaggio, riprende la traccia originale invece di iniziarne una nuova.

## Strumentazione dei Database

Per evitare "punti ciechi" nell'osservabilità, abbiamo strumentato i database per includere ogni comando SQL come uno "span" nella traccia:
- **PostgreSQL (Marten)**: Le query verso il Backoffice DB sono visibili con dettagli sull'operazione Marten.
- **SQL Server**: Tutte le query EF Core e SqlClient per la delivery sono tracciate, inclusi i tempi di esecuzione e lo stato di successo.

## Filtraggio del Rumore Infrastrutturale

Per mantenere la Dashboard pulita e focalizzata sulla logica di business, Pollon implementa un sistema avanzato di filtraggio del "rumore" generato dalle attività di background (polling, leader election, maintenance).

### Strategia di Filtraggio Trace
Il progetto `Pollon.ServiceDefaults` utilizza un **`NoisySpansProcessor`** (custom OTel Processor) che analizza gli span nella fase `OnEnd`. Questo permette di filtrare le query Npgsql basandosi sul loro contenuto SQL, anche se i tag vengono aggiunti dopo l'inizio dello span.

Vengono scartati automaticamente gli span che contengono:
- **Pattern di Database**: `pg_catalog` (query di sistema), `advisory` (lock di sistema Postgres), `mt_` (tabelle interne Marten).
- **Pattern Wolverine**: `wolverine_`, `wolverine.persistence`, `wolverine.polling`.
- **Pattern Marten**: `mt_node_config`.

### Strategia di Filtraggio Log
Per evitare che i log di esecuzione dei comandi SQL inondino la console, il sistema alza il livello minimo di log a `Warning` per le categorie infrastrutturali:
- `Npgsql.Command`: Nasconde i log "Command execution completed".
- `Microsoft.EntityFrameworkCore.Database.Command`: Nasconde il rumore di EF Core.
- `Wolverine`: Filtra i log interni del bus di messaggistica.

## Come tracciare una richiesta

Per vedere il percorso completo di una chiamata, segui questi passi nella **Dashboard di Aspire**:

1.  Apri la voce **Traces** dal menu laterale.
2.  Esegui un'azione nell'app (es: pubblica un articolo).
3.  Cerca la traccia corrispondente (es: `POST /api/content-items/{id}/publish`).
4.  Clicca su **View Details**.

### Esempio di percorso di una chiamata di pubblicazione:
1.  `frontend-web`: L'utente preme "Pubblica".
2.  `backofficeapi`: Riceve la richiesta HTTP, valida i dati e scrive su **PostgreSQL**.
3.  `backofficeapi (Wolverine)`: Invia il messaggio `ContentPublishedEvent` a RabbitMQ.
4.  `contentapi (Wolverine)`: Riceve l'evento, esegue il rendering Scriban e scrive su **SQL Server** e **MinIO**.

Tutti questi passaggi appariranno come un'unica timeline continua, permettendoti di identificare esattamente dove si verificano eventuali ritardi o errori.

## Monitoraggio delle Performance (Metrics)
Oltre alle tracce, il progetto `Pollon.ServiceDefaults` espone metriche standard:
- Utilizzo CPU/Memoria per ogni pod.
- Numero di richieste HTTP al secondo.
- Latenza media delle chiamate database.

Questi dati sono consultabili nella sezione **Metrics** della dashboard.

## Analisi Avanzata con Jaeger

Per un'analisi ancora più dettagliata, abbiamo integrato **Jaeger**, un tool open-source specializzato nel Distributed Tracing.

### Perché usare Jaeger invece della Dashboard Aspire?
Mentre la dashboard di Aspire è ottima per una visione rapida, Jaeger offre:
- **Grafico delle Dipendenze**: Visualizza automaticamente come i tuoi microservizi interagiscono tra loro.
- **Confronto Tracce**: Permette di mettere a confronto due esecuzioni della stessa operazione per capire perché una è stata più lenta dell'altra.
- **Ricerca Avanzata**: Filtri potenti per tag, durata e errori.

### Accesso a Jaeger
1. Avvia la soluzione con `aspire run`.
2. Nella dashboard di Aspire, individua la risorsa **jaeger**.
3. Clicca sul link nell'endpoint **ui** (porta 16686).
4. Seleziona un servizio dal menu "Service" e clicca su "Find Traces" per iniziare l'analisi.
