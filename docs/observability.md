# Osservabilità e Tracciamento Distribuito

Questo documento descrive come Pollon implementa l'osservabilità (Distributed Tracing, Logging e Metrics) per permettere il monitoraggio e il debugging end-to-end delle richieste in un ambiente a microservizi.

## Architettura di Tracciamento

Pollon utilizza **OpenTelemetry (OTel)** come standard per la raccolta di telemetria. Ogni componente della soluzione è strumentato per emettere segnali che vengono raccolti dal dashboard di .NET Aspire durante lo sviluppo.

### Propagazione del TraceId
Quando una richiesta entra nel sistema (es: tramite il Frontend), viene generato un **TraceId** univoco. Questo ID viene propagato automaticamente attraverso tutti i confini del servizio:
1.  **Chiamata HTTP**: Il `TraceId` viene inserito nell'header standard `traceparent`.
2.  **Messaggistica Asincrona (Wolverine)**: Wolverine include il `TraceId` nei metadati del messaggio RabbitMQ. Quando il consumer nel `Content.Api` riceve il messaggio, riprende la traccia originale invece di iniziarne una nuova.

## Strumentazione dei Database

Per evitare "punti ciechi" nell'osservabilità, abbiamo strumentato i database per includere ogni comando SQL come uno "span" nella traccia:
- **PostgreSQL (Marten)**: Le query verso il Backoffice DB sono visibili con dettagli sull'operazione Marten.
- **SQL Server**: Tutte le query EF Core e SqlClient per la delivery sono tracciate, inclusi i tempi di esecuzione e lo stato di successo.

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
