# Pollon

Pollon è un Content Management System (CMS) moderno e distribuito, progettato per offrire una separazione netta tra la gestione dei contenuti (Backoffice) e la loro erogazione (Delivery). Il progetto è costruito su **.NET Aspire** e sfrutta un'architettura a microservizi altamente scalabile.

## Architettura del Sistema

Il sistema è orchestrato da **.NET Aspire** ed è composto dai seguenti componenti:

- **Pollon.AppHost**: Il progetto di orchestrazione che gestisce il service discovery, la configurazione delle risorse (database, messaging, auth) e il ciclo di vita dei container.
- **Pollon.Backoffice.Api**: REST API core per la gestione di tipi di contenuto dinamici ed elementi. Utilizza **Marten** su **PostgreSQL** come database a documenti, permettendo schemi flessibili senza migrazioni SQL.
- **Pollon.Backoffice.Web**: Applicazione amministrativa basata su **Blazor Server** e **MudBlazor**. Gestisce la configurazione dei contenuti, il caricamento media e la pubblicazione.
- **Pollon.Media.Api**: Microservizio dedicato alla gestione degli asset multimediali. Gestisce l'upload e serve i contenuti binari archiviati in PostgreSQL via Marten. Funge da CDN interna, completamente svincolato dalla logica di business del Backoffice.
- **Pollon.Content.Api**: API di delivery ad alte prestazioni. Utilizza **SQL Server** ed **EntityFramework Core** per i modelli di lettura (Read Models), sincronizzati in tempo reale tramite eventi.
- **Pollon.Frontend.Web**: Sito consumer basato su Blazor con routing ottimizzato SEO e gestione gerarchica degli slug.
- **Pollon.Contracts**: Libreria di modelli e record condivisi per la comunicazione cross-service e gli eventi RabbitMQ.
- **Pollon.ServiceDefaults**: Configurazioni standard per osservabilità, telemetria e health checks.

## Stack Tecnologico

- **Orchestrazione**: .NET Aspire
- **Database (Backoffice & Media)**: PostgreSQL + Marten (Document Database)
- **Database (Content/Read Models)**: SQL Server (Entity Framework Core)
- **Messaggistica**: Wolverine + RabbitMQ (Sincronizzazione event-driven)
- **Autenticazione**: Keycloak (OIDC / OAuth2)
- **UI Framework**: MudBlazor & Blazor Server

## Prerequisiti

Per eseguire l'ambiente di sviluppo:

1. **.NET 10 SDK** (o versione superiore)
2. **Docker Desktop**: Necessario per i container SQL Server, PostgreSQL, RabbitMQ e Keycloak.
3. **Aspire Workload**: `dotnet workload install aspire`
4. **EF Core Tooling**: `dotnet tool install --global dotnet-ef`

## Avvio Rapido

1. Clona il repository.
2. Assicurati che Docker Desktop sia attivo.
3. Imposta `Pollon.AppHost` come progetto di avvio in Visual Studio o lancia da terminale:
   ```bash
   dotnet run --project Pollon.AppHost/Pollon.AppHost.csproj
   ```

## Autenticazione e Sicurezza

Il sistema utilizza **Keycloak** per la gestione centralizzata delle identità.

### Credenziali di Sviluppo
- **Console Admin Keycloak**:
  - **User**: `admin`
  - **Password**: `admin`
- **Accesso Backoffice Web**:
  - **User**: `talco`
  - **Password**: `talco`

### Gestione dei Volumi
In caso di modifiche al realm o necessità di reset totale:
1. Ferma l'applicazione.
2. Elimina la cartella `./Pollon.AppHost/keycloak-data`.
3. Al riavvio, il sistema re-importerà automaticamente la configurazione da `realm-export.json`.

## Funzionalità Core

- **Tipi di Contenuto Dinamici**: Definizione di strutture dati (Testo, Numeri, Immagini, etc.) via UI senza modifiche al database fisico.
- **Media Management**: Upload centralizzato con proxying trasparente verso il microservizio media.
- **Sincronizzazione Event-Driven**: Ogni modifica nel backoffice produce un evento RabbitMQ che aggiorna asincronamente i modelli di lettura nel Content API.
- **Gerarchia di Contenuti**: Supporto nativo per relazioni parent-child e gestione slug SEO.

## Documentazione Tecnica
- [Marten Database Schema](docs/marten_db_schema.md)
- [Publication Modes & SSG](docs/publication-modes.md)
- [Autenticazione Backoffice (Walkthrough)](walkthrough.md)
- [Ordinamento Campi Content Type (Walkthrough)](walkthrough-field-ordering.md)
