# Architettura dei Dati e Strategia di Ricerca

Questo documento descrive le scelte architettoniche relative alla persistenza dei dati e alla gestione della ricerca all'interno di Pollon.

## Strategia Database Ibrida (CQRS)

Pollon adotta un'architettura ispirata al pattern **CQRS (Command Query Responsibility Segregation)**, utilizzando database differenti per le operazioni di scrittura (Backoffice) e quelle di lettura/distribuzione (Delivery).

### 1. Write Model: PostgreSQL + Marten (Backoffice)
Il database del Backoffice utilizza **PostgreSQL** con la libreria **Marten**.
- **Perché Marten?**: Marten trasforma PostgreSQL in un potente *Document Store*.
- **Flessibilità**: In fase di editing, la struttura dei contenuti (es: schemi flessibili dei tipi di contenuto) può evolvere senza la necessità di migrazioni SQL complesse e rigide.
- **Integrità**: Beneficiamo comunque della robustezza e delle transazioni ACID di PostgreSQL.

### 2. Read Model: SQL Server (Content Delivery)
Il database di delivery (Content API) utilizza **SQL Server**.
- **Scopo**: Questo database è ottimizzato per la velocità di lettura e serve il Frontend tramite API.
- **Decoupling**: La separazione garantisce che un carico elevato di visualizzazione sul sito web non influenzi le prestazioni del Backoffice.
- **Relational Power**: SQL Server offre ottime prestazioni per query strutturate e filtraggio dei contenuti pubblicati.

---

## Strategie di Ricerca a Confronto

Pollon utilizza due approcci differenti per la ricerca, ottimizzati per le diverse esigenze degli utenti del Backoffice rispetto ai visitatori del Frontend.

### 1. Backoffice Search: Potenzialità di PostgreSQL (Ngram Index)
Nel Backoffice, la necessità principale è trovare rapidamente contenuti in fase di editing, supportando ricerche parziali e intuitive ("search-as-you-type").
- **Tecnologia**: PostgreSQL + Marten.
- **Ngram Index**: Abbiamo configurato un indice di tipo **Ngram** sul campo `SearchText`. Questo permette al database di frammentare le parole in sequenze di caratteri (bi-grammi, tri-grammi), rendendo le ricerche per sottostringhe estremamente efficienti.
- **Vantaggio**: È possibile trovare un articolo cercando solo una parte di una parola contenuta in un campo JSON dinamico, con performance notevolmente superiori a un semplice comando `LIKE`.

### 2. Delivery Search: Unified Search Text (SQL Server)
Per i visitatori del sito (Frontend), la ricerca deve essere veloce e scalabile, ma spesso meno "granulare" rispetto alla fase di editing.
- **Tecnologia**: SQL Server.
- **Approccio**: Anche qui usiamo una denormalizzazione in `SearchText`, ma basata su indici testuali standard.
- **Integrazione**: Il campo viene popolato durante l'evento di pubblicazione, garantendo che i dati di ricerca siano sempre sincronizzati con l'ultima versione approvata del contenuto.

### Perché questa distinzione?
Questa architettura permette di sfruttare il meglio di entrambi i mondi:
- **PostgreSQL** nel Backoffice gestisce la complessità dei dati semistrutturati (JSONB) e la ricerca avanzata durante la creazione.
- **SQL Server** nella Delivery garantisce stabilità e velocità di risposta per le query di massa orientate alla visualizzazione.

### Evoluzione Futura
L'architettura è predisposta per scalare: se i volumi di dati o la complessità delle query dovessero aumentare, il consumer che popola il campo `SearchText` può essere facilmente aggiornato per inviare i dati a un motore di ricerca dedicato (es. OpenSearch) senza impattare il resto del sistema.
