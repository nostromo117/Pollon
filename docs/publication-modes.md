# Pollon CMS: Dual Publication Modes & Static Site Generation (SSG)

Questa documentazione illustra l'architettura e il funzionamento del sistema di pubblicazione a due vie di Pollon, introdotto nella Fase 2.

## Architettura del Sistema

Pollon supporta due modalità di pubblicazione, definibili per ogni **ContentType** nel Backoffice:

1.  **Headless (JSON)**: Il contenuto viene salvato in formato grezzo e servito via API REST. Ideale per app mobile o client JavaScript che gestiscono il rendering.
2.  **Static (HTML)**: Il contenuto viene pre-renderizzato sul server al momento della pubblicazione e servito come blocco HTML pronto o come file statico.

---

## 1. Rendering con Scriban

Il motore di rendering utilizzato è **Scriban**. 

-   **Template**: I template si trovano nella cartella `/Templates` del progetto `Pollon.Content.Api`.
-   **Logica**: Al momento della pubblicazione (evento `ContentPublishedEvent`), il microservizio `Content.Api` intercetta l'evento, recupera i dati, e applica il template scelto (default: `default.sbn`).

### Oggetto Dati (Context)
Il template Scriban ha accesso a un oggetto arricchito:
-   `id`, `title`, `slug`, `published_at`, `content_type`: Metadati principali.
-   Tutti i campi dinamici definiti dall'utente nel Backoffice.
-   `images`: Una lista di asset (URL e Alt) se è presente una galleria associata.

---

## 2. SSG con MinIO (Object Storage)

Per massimizzare la scalabilità, Pollon implementa una funzione di **Static Site Generation (SSG)** sfruttando **MinIO**.

-   **Destinazione**: Ogni volta che un contenuto viene pubblicato in modalità `Static` o `Both`, l'HTML generato viene caricato in un Bucket chiamato `pollon-static`.
-   **Policy Pubblica**: Il bucket è configurato per l'accesso pubblico in sola lettura.
-   **Naming Convention**: I file vengono salvati usando lo slug come nome (es: `articoli/mio-viaggio.html`).

Questo approccio permette di scaricare il carico dal DB SQL e di servire le pagine direttamente da una CDN o dallo storage statico verso l'utente finale.

---

## 3. Ottimizzazione della Ricerca

La ricerca nella Content API è stata separata dalla logica di storage JSON:

-   **Campo `SearchText`**: È un campo dedicato nel database di delivery che contiene tutto il testo indicizzabile del contenuto, privo di metadati tecnici o tag JSON.
-   **Efficienza**: Le API di ricerca interrogano esclusivamente questo campo, migliorando drasticamente la precisione dei risultati e le performance delle query SQL.

---

## 4. Come Utilizzarlo

1.  **Backoffice**: Crea o modifica un `ContentType`.
2.  **Configura**: Scegli "Publish Mode" = `Both` e "Template Name" = `default`.
3.  **Pubblica**: Salva un elemento.
4.  **Verifica**: 
    -   API: `/api/content/item/{id}` (JSON + HTML).
    -   Storage: Vai sulla Console MinIO per vedere il file fisico.
    -   Frontend: La pagina articolo visualizzerà automaticamente l'output renderizzato.
