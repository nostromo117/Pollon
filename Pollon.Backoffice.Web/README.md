# Pollon Backoffice Web

Interfaccia di amministrazione per la gestione dei contenuti del CMS Pollon. Costruita con **Blazor Server** e **MudBlazor**, si connette al servizio `Pollon.Backoffice.Api` per tutte le operazioni CRUD e di pubblicazione.

---

## Indice

- [Autenticazione](#autenticazione)
- [Sezione CONTENT](#sezione-content)
  - [Content Items](#content-items)
  - [Content Types](#content-types)
  - [Content Templates](#content-templates)
    - [Template Code inline](#template-code-inline)
    - [Variabili di configurazione](#variabili-di-configurazione)
    - [Tag e stato attivo](#tag-e-stato-attivo)
- [Sezione MEDIA](#sezione-media)
  - [Gallerie Media](#gallerie-media)
- [Sezione SETTINGS](#sezione-settings)
  - [Plugins](#plugins)
- [Modalità di Pubblicazione](#modalità-di-pubblicazione)
- [Ciclo di Vita di un Contenuto](#ciclo-di-vita-di-un-contenuto)

---

## Autenticazione

Il backoffice usa **Keycloak** come Identity Provider. L'accesso è protetto da OAuth2/OIDC: ogni pagina richiede l'attributo `[Authorize]` e il token viene gestito automaticamente dal `TokenProvider` con refresh automatico in caso di scadenza.

```
URL Backoffice:  https://localhost:<porta>
Realm Keycloak:  Pollon
Client ID:       backoffice
```

Al primo accesso si viene reindirizzati alla login page di Keycloak. Dopo l'autenticazione si viene riportati al backoffice con il token attivo.

---

## Sezione CONTENT

### Content Items

**URL:** `/content-items`

Gestione di tutti i contenuti del CMS. La lista è organizzata ad **albero gerarchico**: i contenuti possono avere relazioni padre-figlio (es. categorie con articoli annidati).

#### Funzionalità della lista

| Funzione | Descrizione |
|---|---|
| **Ricerca fuzzy** | Filtra i contenuti per testo in tempo reale con debounce da 400ms |
| **Filtro per Status** | Visualizza solo Draft, Published o Archived |
| **Vista ad albero** | Espandi/comprimi i nodi figli con toggle |
| **Icone di stato** | Icona colorata in blu per i contenuti Published |
| **Warning badge** | Icona gialla con tooltip se il contenuto ha avvisi (es. slug mancante) |
| **Edit / Delete** | Bottoni inline su ogni riga |

#### Creare un nuovo Content Item

1. Clicca **New Content** in alto a destra
2. Seleziona il **Content Type** (es. `blog-post`, `product`)
3. I campi si generano dinamicamente in base alla definizione del tipo
4. Compila i valori nei campi
5. Nella colonna destra imposta **Status** e **Publish Mode**
6. (Opzionale) Imposta uno **Slug** personalizzato per l'URL
7. (Opzionale) Seleziona un'**icona** MUI per identificare visivamente il contenuto nell'albero
8. Clicca **Save Content**

> Se il contenuto viene salvato con Status = `Published`, viene inviato automaticamente un evento su RabbitMQ che avvia il processo di pubblicazione nel `Content.Api`.

#### Struttura del form di editing

```
┌─────────────────────┬──────────────────────────────────┐
│  Navigation (tree)  │  Configuration                   │
│  ─────────────────  │  ├─ Content Type (select)         │
│  [Albero dei        │  └─ Campi dinamici                │
│   contenuti con     ├──────────────────────────────────┤
│   quick-add]        │  Publishing                      │
│                     │  ├─ Status (Draft/Published/...)  │
│                     │  ├─ Publication Mode (override)   │
│                     │  ├─ Slug                          │
│                     │  ├─ Icon picker                   │
│                     │  └─ Created/Updated/Published At  │
└─────────────────────┴──────────────────────────────────┘
```

#### Tipi di campo supportati

| Tipo | Componente UI | Note |
|---|---|---|
| `Text` | TextField | Input testo singola riga |
| `RichText` | TextField multiriga | Supporta HTML, 5 righe |
| `Number` | TextField numerico | Input type=number |
| `Boolean` | Switch | Toggle on/off |
| `Date` | TextField data | Input type=date |
| `Image` | Upload + preview | Upload diretto su Media API, preview con skeleton |

#### Navigazione nell'albero (sidebar sinistra)

Nella pagina di editing è presente una sidebar con l'albero completo dei contenuti per navigare rapidamente tra gli item senza tornare alla lista. Ogni nodo ha un pulsante `+` per aggiungere un figlio direttamente.

**Esempio pratico — creare una struttura blog:**

```
📁 Blog                          (ContentType: category, Status: Draft)
  └── 📄 Benvenuto nel Blog      (ContentType: blog-post, Status: Published)
  └── 📄 Come usare Pollon CMS   (ContentType: blog-post, Status: Draft)
```

1. Crea l'item `Blog` con tipo `category`
2. Nell'albero, clicca `+` accanto a `Blog`
3. Viene creato automaticamente un figlio con `ParentId` = ID di `Blog`
4. Compila il titolo e pubblica

---

### Content Types

**URL:** `/content-types`

I Content Types definiscono la **struttura dei dati** che ogni Content Item di quel tipo deve avere. Sono analoghi agli "schemi" o "modelli" in altri CMS.

#### Creare un Content Type

1. Clicca **New Content Type**
2. Compila:
   - **Display Name**: nome leggibile (es. `Articolo Blog`)
   - **System Name**: identificatore interno senza spazi (es. `ArticoloBlog`) — viene suggerito automaticamente dal Display Name
   - **Slug**: prefisso URL per i contenuti di questo tipo (es. `blog`) — genera URL del tipo `/blog/<slug-item>`
   - **Description**: descrizione opzionale
   - **Publish Mode**: `Headless`, `Static` o `Both` (vedi [Modalità di Pubblicazione](#modalità-di-pubblicazione))
   - **Template**: dropdown con i template grafici disponibili (abilitato se il Publish Mode non è Headless)
3. Aggiungi i campi con il pulsante **Add Field**
4. Per ogni campo specifica: nome, tipo, obbligatorietà e posizione (riordinabile con frecce su/giù)
5. Clicca **Save Content Type**

#### Esempio pratico — definire un tipo "Articolo Blog"

| Campo | Tipo | Obbligatorio |
|---|---|---|
| Titolo | Text | ✅ |
| Sommario | Text | ❌ |
| Corpo | RichText | ✅ |
| Immagine copertina | Image | ❌ |
| Data evento | Date | ❌ |
| In evidenza | Boolean | ❌ |

Dopo aver salvato, ogni nuovo Content Item con questo tipo mostrerà esattamente questi campi nel form di editing.

#### Esempio pratico — definire un tipo "Prodotto" (pubblicazione statica)

```
Display Name:  Prodotto
System Name:   Prodotto
Slug:          prodotti
Publish Mode:  Static
Template:      magazine.sbn

Campi:
  - Nome         (Text,    required)
  - Descrizione  (RichText, required)
  - Prezzo       (Number,  required)
  - Foto         (Image,   optional)
  - Disponibile  (Boolean, required)
```

Quando un item di tipo `Prodotto` viene pubblicato, il `Content.Api` renderizza il template `magazine.sbn` con i dati dell'item e salva il file HTML su MinIO.

---

### Content Templates

**URL:** `/content-templates`

Registro dei template grafici `.sbn` (Scriban) disponibili per la pubblicazione statica. Ogni template può essere basato su un **file fisico** nella cartella `Templates/` del `Pollon.Content.Api`, oppure avere il **codice inline** salvato direttamente nel database e modificabile dal backoffice senza deploy.

Il dialog di creazione/modifica è organizzato in **tre tab**:

| Tab | Contenuto |
|---|---|
| **Generale** | Nome, file di riferimento, descrizione, tag, URL anteprima, switch attivo/inattivo |
| **Variabili** | Dizionario chiave/valore iniettato nel contesto Scriban come `{{ vars.chiave }}` |
| **Template Code** | Sorgente HTML/Scriban inline — ha **priorità** sul file `.sbn` su disco |

#### Template predefiniti disponibili

| File | Nome | Stile |
|---|---|---|
| `default.sbn` | Default | Bootstrap classico, layout semplice |
| `magazine.sbn` | Magazine | Rivista editoriale, 2 colonne con sidebar, font Playfair Display |
| `minimal.sbn` | Minimal | Tipografico, colonna singola, font Cormorant Garamond, barra di lettura |
| `dark-card.sbn` | Dark Card | Dark mode tech, card per campo, font Space Grotesk, accenti neon |

#### Registrare un template basato su file

1. **Crea il file** `<nome>.sbn` nella cartella `Pollon.Content.Api/Templates/`
2. Nel backoffice vai su **Content Templates** → **New Template**
3. Tab **Generale** — compila:
   - **Nome visualizzato**: es. `Landing Page Moderna`
   - **Nome file template**: es. `landing-modern.sbn` (deve corrispondere esattamente al file)
   - **Descrizione** e **Tag** opzionali
4. Lascia vuoto il tab **Template Code**
5. Salva

#### Template Code inline

Il tab **Template Code** permette di scrivere il sorgente HTML/Scriban direttamente nel backoffice, senza toccare il filesystem. Il template inline ha sempre precedenza sul file `.sbn` indicato nel campo "Nome file template".

**Esempio di template inline minimale:**

```html
<!DOCTYPE html>
<html>
<head>
  <title>{{ title }}</title>
  <style>
    body { background: {{ vars.bg_color }}; font-family: {{ vars.font_family }}; }
    h1   { color: {{ vars.accent_color }}; }
  </style>
</head>
<body>
  <h1>{{ title }}</h1>
  <p>Pubblicato il {{ published_at | date.to_string "%d/%m/%Y" }}</p>
  {{ for img in images }}
    <img src="{{ img.url }}" alt="{{ img.alt }}" />
  {{ end }}
  {{ for pair in this }}
    {{ if pair.key != "title" && pair.key != "id" && pair.key != "images" }}
      <div><strong>{{ pair.key }}:</strong> {{ pair.value }}</div>
    {{ end }}
  {{ end }}
</body>
</html>
```

#### Variabili di configurazione

Le variabili sono coppie chiave/valore definite nel tab **Variabili** e iniettate nel contesto Scriban sotto il namespace `vars`. Permettono di cambiare l'aspetto del template (colori, font, ecc.) senza modificarne il codice sorgente.

**Esempio — creare un template "Branded" con variabili:**

| Chiave | Valore |
|---|---|
| `bg_color` | `#1a1a2e` |
| `accent_color` | `#e94560` |
| `font_family` | `'Inter', sans-serif` |
| `logo_url` | `https://cdn.example.com/logo.png` |

Nel codice Scriban:

```scriban
<style>
  body  { background: {{ vars.bg_color }}; font-family: {{ vars.font_family }}; }
  h1    { color: {{ vars.accent_color }}; }
</style>
<img src="{{ vars.logo_url }}" alt="Logo" />
```

Modificando le variabili dal backoffice, il prossimo contenuto pubblicato con quel template userà i nuovi valori senza deploy.

#### Variabili di sistema sempre disponibili

```scriban
{{ title }}          # Titolo dell'item
{{ slug }}           # Slug dell'item
{{ published_at }}   # Data di pubblicazione (DateTime)
{{ content_type }}   # Display name del Content Type
{{ id }}             # ID univoco dell'item
{{ images }}         # Lista immagini della galleria (array con .url e .alt)
{{ vars.chiave }}    # Variabili personalizzate definite nel template
```

#### Tag e stato attivo

- **Tag**: etichette libere (es. `blog, magazine, dark`) per categorizzare i template nella lista.
- **Attivo/Inattivo**: un template inattivo non compare nel dropdown di selezione dei Content Types ma rimane nel registro. Utile per nascondere template in manutenzione senza eliminarli.

Le card nella lista mostrano un badge **Inline** (verde) se il template ha codice inline, i tag come chip e il numero di variabili configurate.

---

## Sezione MEDIA

### Gallerie Media

**URL:** `/galleries`

Gestione delle raccolte di immagini da associare ai Content Items.

#### Creare una galleria

1. Clicca **Nuova Galleria**
2. Assegna un nome alla galleria
3. Carica le immagini (upload verso il `Media.Api`, archiviazione su MinIO)
4. Imposta lo stato: **Bozza** o **Pubblicata**
5. Salva

Una galleria pubblicata può essere associata a un Content Item tramite il campo `GalleryId`. Quando l'item viene pubblicato, il `Content.Api` recupera automaticamente le immagini della galleria e le inietta nella variabile `images` del template.

#### Esempio pratico — galleria per un articolo

```
Galleria: "Foto Evento Marzo 2025"  →  Stato: Pubblicata
  ├── img1.jpg  (ID: abc123)
  ├── img2.jpg  (ID: def456)
  └── img3.jpg  (ID: ghi789)
```

Nel template Scriban:

```scriban
{{ for img in images }}
  <img src="{{ img.url }}" alt="{{ img.alt }}" />
{{ end }}
```

---

## Sezione SETTINGS

### Plugins

**URL:** `/plugins`

Gestione delle identità dei plugin esterni che si integrano con il CMS tramite Keycloak e Consul.

#### Cosa sono i Plugin

I plugin sono microservizi esterni che si registrano automaticamente su Consul al loro avvio. Possono ricevere eventi di pubblicazione e operare su specifici Content Types. Ogni plugin usa un Client ID Keycloak per autenticarsi con il backoffice.

#### Creare un'identità plugin

1. Clicca **Create New Identity**
2. Inserisci il nome del plugin (es. `email-notifier`)
3. Il sistema crea automaticamente un client in Keycloak e restituisce:
   - **Client ID**
   - **Client Secret**
4. ⚠️ **Copia subito le credenziali**: il secret non verrà mostrato di nuovo
5. Inserisci le credenziali nella configurazione del plugin

#### Informazioni mostrate nella lista

| Colonna | Descrizione |
|---|---|
| **Status** | `online` / `offline` (rilevato tramite Consul) |
| **Name** | Nome del plugin |
| **ID / Instance** | ID del plugin e Consul Service ID dell'istanza |
| **Enabled types** | Content Types su cui il plugin è abilitato ad operare |
| **Last Seen** | Ultima volta che il plugin ha comunicato con il sistema |

#### Azioni disponibili

- **Configura**: modifica i Content Types abilitati per il plugin
- **Rigenera secret**: genera un nuovo Client Secret Keycloak
- **Elimina identità**: rimuove il client da Keycloak

---

## Modalità di Pubblicazione

Ogni Content Type (e ogni singolo Content Item tramite override) può avere una delle seguenti modalità:

| Modalità | Descrizione | Output |
|---|---|---|
| **Headless** | Solo dati JSON via API | Nessun file HTML generato. Il contenuto è disponibile tramite `GET /api/content/{slug}` |
| **Static** | Rendering HTML tramite template Scriban | File `.html` salvato su MinIO, accessibile via CDN/proxy |
| **Both** | Headless + Static | Sia API JSON che file HTML generati |

L'override sul singolo Content Item permette di cambiare la modalità rispetto al default del tipo, senza modificare il Content Type.

---

## Ciclo di Vita di un Contenuto

```
[Backoffice Web]
      │
      │  1. Utente crea/modifica un Content Item
      │     e imposta Status = "Published"
      │
      ▼
[Backoffice API]
      │
      │  2. Salva l'item su PostgreSQL (Marten)
      │  3. Pubblica evento ContentPublishedEvent su RabbitMQ
      │
      ▼
[Content API - Consumer]
      │
      │  4. Recupera i dati completi dell'item (ContentType, Gallery)
      │  5. Serializza i dati in JSON
      │  6. Se mode = Static | Both:
      │       - Risolve il ContentTemplate dal registro (by-filename)
      │       - Se il template ha TemplateContent inline → usa quello
      │       - Altrimenti legge il file .sbn da disco
      │       - Inietta le Variables del template come {{ vars.* }}
      │       - Renderizza con Scriban
      │       - Salva il file HTML su MinIO
      │  7. Salva/aggiorna il record in PostgreSQL (contentdb)
      │
      ▼
[Content API - Endpoints]
      │
      │  GET /api/content/         → lista paginata
      │  GET /api/content/{slug}   → per slug (es. /blog)
      │  GET /api/content/item/{id} → singolo item
      │
      ▼
[Frontend / Plugin / CDN]
```

---

## Struttura del Progetto

```
Pollon.Backoffice.Web/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor        # Layout principale con MudBlazor drawer
│   │   └── NavMenu.razor           # Menu di navigazione laterale (sezioni CONTENT, MEDIA, SETTINGS)
│   └── Pages/
│       ├── Home.razor              # Dashboard
│       ├── ContentItems.razor      # Lista ad albero dei content items (ricerca, filtri, CRUD)
│       ├── EditContentItem.razor   # Form creazione/modifica content item con campi dinamici
│       ├── ContentTypes.razor      # Lista dei content types
│       ├── EditContentType.razor   # Form creazione/modifica content type + selezione template
│       ├── ContentTemplates.razor  # Registro template grafici (Generale / Variabili / Template Code)
│       ├── MediaGalleries.razor    # Lista gallerie media
│       ├── EditMediaGallery.razor  # Form galleria con upload immagini
│       └── Plugins.razor           # Gestione plugin e identità Keycloak
├── Services/
│   ├── IContentTreeService.cs      # Interfaccia per la gestione dell'albero
│   └── ContentTreeService.cs       # Logica di costruzione albero gerarchico
├── BackofficeApiClient.cs          # HTTP client verso Backoffice.Api (CRUD + template)
├── TokenProvider.cs                # Gestione token OAuth2 + refresh
└── Program.cs                      # Bootstrap dell'applicazione
```

### Modelli condivisi (`Pollon.Publication`)

```
Pollon.Publication/Models/
├── ContentItem.cs        # Item di contenuto con campi dinamici, slug, status, GalleryId
├── ContentType.cs        # Schema dei campi + PublishMode + TemplateName
├── ContentTemplate.cs    # Template grafico con TemplateContent inline, Variables, Tags, IsActive
├── ContentField.cs       # Definizione di un campo (nome, tipo, posizione, required)
├── MediaGallery.cs       # Galleria con lista AssetId
└── PublishedContent.cs   # Record pubblicato su PostgreSQL (slug, html, json, mode)
```
