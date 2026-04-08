# Ciclo di Vita della Pubblicazione in Pollon

Questo documento descrive il flusso end-to-end di come un contenuto viene creato nel Backoffice, processato e infine reso disponibile nel Frontend attraverso un'architettura guidata dagli eventi (Event-Driven).

## Panoramica del Flusso

Il sistema di pubblicazione di Pollon è completamente asincrono e si basa su RabbitMQ (gestito tramite Wolverine) per la comunicazione tra microservizi.

### 1. Fase di Trigger (Backoffice API)
Quando un utente preme "Salva e Pubblica" nel Backoffice:
1. Il `ContentItemService` salva i dati grezzi nel database PostgreSQL.
2. Viene emesso un evento `ContentPublishedEvent` o `ContentUpdatedEvent` contenente solo l'ID del contenuto.

### 2. Fase di Elaborazione (Content API)
Il microservizio `Content.Api` è in ascolto di questi eventi:
1. **Recupero Dati**: Il consumer chiama il Backoffice API per ottenere i dati completi dell'item e le informazioni sul suo `ContentType`.
2. **Rendering**: Se il tipo di contenuto prevede la modalità `Static` o `Both`, viene invocato lo `ScribanTemplateRenderer`.
3. **SSG (Static Site Generation)**: L'HTML prodotto viene inviato al `MinioStaticStorage`.
4. **Persistenza Delivery**: I metadati, il testo per la ricerca (`SearchText`) e il blocco HTML vengono salvati nel database SQL Server dedicato alla lettura.

## Diagrammi di Sequenza

### Flusso di Pubblicazione Contenuto

```mermaid
sequenceDiagram
    participant BO as Backoffice UI (Blazor)
    participant BOAPI as Backoffice API
    participant RMQ as RabbitMQ (Wolverine)
    participant CAPI as Content API
    participant MINIO as MinIO Storage
    participant SQL as SQL Server (Read Model)

    BO->>BOAPI: Crea/Aggiorna Contenuto (Stato: Published)
    BOAPI->>BOAPI: Salva in PostgreSQL
    BOAPI-->>RMQ: Pubblica ContentPublishedEvent(ID)
    BO-->>BO: Notifica successo UI

    Note over RMQ, CAPI: Elaborazione Asincrona
    RMQ->>CAPI: Consegna Evento al Consumer
    CAPI->>BOAPI: GET /api/content-items/{id} (Arricchimento)
    BOAPI-->>CAPI: Ritorna Dati Completi + ContentType
    
    CAPI->>CAPI: Scriban Rendering (HTML Generation)
    
    par Salvataggio Distribuiti
        CAPI->>MINIO: Upload File .html (SSG)
        CAPI->>SQL: Salva Metadati + HTML + SearchText
    end

    Note right of SQL: Contenuto pronto per il Frontend
```

### Flusso di Visualizzazione (Frontend + YARP Proxy)

```mermaid
sequenceDiagram
    participant USER as Utente Finale
    participant FRONT_P as Frontend (YARP Proxy)
    participant FRONT_B as Frontend (Blazor Server)
    participant CAPI as Content API
    participant MINIO as MinIO Storage
    participant SQL as SQL Server (Read Model)

    alt Richiesta Pagina Dinamica (/article/{slug})
        USER->>FRONT_B: Naviga su /article/{slug}
        FRONT_B->>CAPI: GET /api/content/{slug}
        CAPI->>SQL: Query per Slug
        SQL-->>CAPI: Ritorna PublishedContent
        CAPI-->>FRONT_B: Ritorna JSON (incluso HtmlContent)
        FRONT_B->>FRONT_B: Renderizza MarkupString
        FRONT_B-->>USER: Mostra Pagina dinamica
    else Richiesta Pagina Statica (/published/{slug}.html)
        USER->>FRONT_P: Naviga su /published/{slug}.html
        FRONT_P->>MINIO: Proxy Request (Inoltro a bucket statico)
        MINIO-->>FRONT_P: Ritorna Standalone HTML
        FRONT_P-->>USER: Serve file statico (Zero overhead Blazor)
    end
```

## Reverse Proxy & Ottimizzazione CDN
L'integrazione di **YARP (Yet Another Reverse Proxy)** nel Frontend permette di servire i contenuti direttamente dallo storage ma sotto lo stesso dominio dell'applicazione principale.
- **Vantaggi**: 
    - Coerenza del dominio per SEO e SSL.
    - Facilità di cache: una CDN (es. Cloudflare) può caricare in cache l'intero percorso `/published/*`.
    - Performance: il server Blazor non viene interpellato per il rendering della pagina, riducendo drasticamente l'uso di memoria e CPU.


## Stati e Modalità di Pubblicazione

Il ciclo di vita è influenzato dalla configurazione del **ContentType**:

| Modalità | Azione Content API | Destinazione Principale |
| :--- | :--- | :--- |
| **Headless** | Salva solo JSON | SQL Server (Delivery DB) |
| **Static** | Renderizza HTML e salva file | MinIO Storage |
| **Both** | Esegue SSG e popola API | SQL Server & MinIO |

## Gestione degli Errori e Robustezza
- **Durable Inbox**: Il sistema utilizza il pattern Outbox/Inbox di Wolverine per garantire che nessun evento di pubblicazione vada perso in caso di downtime temporaneo di un servizio.
- **Persistence**: Come configurato in `AppHost.cs`, tutti i database e lo storage statico sono persistenti grazie ai volumi Docker, garantendo la disponibilità dei dati tra i riavvii.
