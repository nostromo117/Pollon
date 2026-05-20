# C# Coding Guidelines

Queste regole definiscono gli standard di stile e le preferenze per il refactoring del codice all'interno di questo progetto.

## Simplify Collection Initialization (IDE0300)
- **Regola**: Utilizzare le *collection expressions* (`[]` o `[.. collection]`) per inizializzare collezioni o array, eliminando le vecchie chiamate `new List<T>()` o `.ToList()`.
- **Perché**: Le espressioni di raccolta introdotte in C# 12 sono più sintetiche, più leggibili e in molti casi meglio ottimizzate dal compilatore.
- **Esempi**:
  - `List<string> items = [];` invece di `List<string> items = new List<string>();` oppure `new()`.
  - `item.Children = [.. children];` invece di `item.Children = children.ToList();`.

## Simplify Null Checks (IDE0029, IDE0270)
- **Regola**: Semplificare i controlli di `null` (quando seguiti dal lancio di un'eccezione) avvalendosi dell'operatore *null-coalescing* (`??`).
- **Perché**: Riduce il rumore visivo (rimuove i blocchi `if` e le parentesi graffe) e mantiene l'inizializzazione o l'assegnazione della variabile in linea con la validazione del suo valore.
- **Esempi**:
  - `var obj = await repo.GetAsync(id) ?? throw new Exception("Not found");` 
  invece di:
  ```csharp
  var obj = await repo.GetAsync(id);
  if (obj == null) { throw new Exception("Not found"); }
  ```

## Architettura e Design Software
- **Onion Architecture / Hexagonal Architecture (Ports and Adapters)**:
  - Mantenere il Core del dominio isolato da dipendenze esterne (es. database, framework web, code di messaggistica).
  - Tutti i riferimenti e le dipendenze devono puntare verso l'interno, verso il Core (Modelli di Dominio, Interfacce dei repository/servizi).
  - L'infrastruttura (Marten, PostgreSQL, Wolverine, Api/Web) si deve appoggiare su interfacce definite dal Core.
- **Principi SOLID - Enfasi sulla Dependency Inversion (DIP)**:
  - Il Core non deve dipendere da dettagli di implementazione (es. `MartenRepository`), ma solo da astrazioni (es. `IRepository<T>`).
  - L'Infrastruttura implementerà queste interfacce (Adapter) assecondando i requisiti (Port) imposti dal dominio.
  - Sfruttare pienamente l'Inversion of Control (IoC) e la Dependency Injection (DI) fornita da ASP.NET Core per iniettare i dettagli di implementazione nei servizi.
- **Repository Pattern per l'accesso ai dati**:
  - Non accedere mai direttamente al `DbContext` (EF Core) o alle sessioni dei database (es. Marten) all'interno di controller, endpoint minimal api, consumer di Wolverine/RabbitMQ o servizi di business logic.
  - Incapsulare tutte le query di lettura e le operazioni di persistenza (inserimenti, aggiornamenti, cancellazioni) all'interno di astrazioni di Repository definite nel Core (es. `IRepository<T>`, `IContentTemplateRepository`).
  - Questo garantisce la testabilità del codice, la separazione dei compiti e facilita l'eventuale sostituzione della tecnologia di persistenza sottostante.
