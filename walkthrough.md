# Walkthrough: Autenticazione Backoffice con Keycloak

L'autenticazione per il Backoffice di Pollon è stata implementata utilizzando **Keycloak** come Identity Provider centralizzato. Il sistema è ora protetto sia a livello di interfaccia utente (Blazor) che di API (JWT).

## Modifiche Principali

### 1. Configurazione Infrastruttura (.NET Aspire)
In `AppHost.cs`, la risorsa Keycloak è stata configurata con:
- **Persistenza**: Utilizzo di un volume Docker (`keycloak-data`) per mantenere utenti e configurazioni anche dopo il riavvio.
- **Importazione Automatica**: All'avvio viene importato il file `realm-export.json` che configura il realm `Pollon`, il client `backoffice` e l'utente `talco`.

### 2. Sicurezza API
Il progetto `Pollon.Backoffice.Api` ora:
- Richiede un **JWT Bearer Token** per tutti gli endpoint di `/api/content-types` e `/api/content-items`.
- Valida l'authority direttamente tramite Keycloak.

### 3. Sicurezza Web App (Blazor)
Il progetto `Pollon.Backoffice.Web` ora:
- Utilizza **OpenID Connect (OIDC)** per l'autenticazione dell'utente.
- **Token Handoff**: Implementato un sistema di bridge (`TokenStateBridge` e `TokenStateRestorer`) per mantenere il JWT tra SSR e SignalR.
- **Client API Autenticato**: Il `BackofficeApiClient` inietta il token direttamente per ogni chiamata.
- Protegge tutte le pagine amministrative tramite l'attributo `[Authorize]`.

### 4. Comunicazione Service-to-Service
Il progetto `Pollon.Content.Api` ora:
- Utilizza un **KeycloakTokenService** (flusso *Client Credentials*) per ottenere un token di servizio.
- Questo permette ai consumatori di eventi di chiamare il `Backoffice.Api` in modo sicuro.

## Come Accedere

Per accedere al Backoffice, utilizza le seguenti credenziali pre-configurate:

- **Username**: `talco`
- **Password**: `talco`

### Interfaccia Utente
Nella barra superiore (`AppBar`) è ora visibile:
- Il nome dell'utente collegato con un menu a tendina.
- Un'opzione per effettuare il **Logout** all'interno del menu.
- Se non sei autenticato, verrai automaticamente reindirizzato alla pagina di login di Keycloak quando tenti di accedere a una sezione protetta.

## Verifica Tecnica Completata
- [x] Keycloak con realm `Pollon` e client `backoffice`/`contentapi`.
- [x] Utente `talco` / `talco` (admin) e credenziali console `admin` / `admin`.
- [x] Bridge dei token funzionante tra SSR e SignalR.
- [x] Flusso di pubblicazione eventi autenticato (Content API -> Backoffice API).
- [x] Logout con `id_token_hint` per pulizia sessione Keycloak.
- [x] Documentazione `README.md` aggiornata.
