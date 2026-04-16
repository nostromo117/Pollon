# Walkthrough: Ordinamento Campi Content Type

Questa modifica permette agli editor di definire l'ordine dei campi (Position) all'interno di un Content Type. L'ordine viene rispettato sia nel Backoffice (durante l'editing dei contenuti) che potenzialmente nei consumer a valle.

## Modifiche Principali

### 1. Modello Dati
Abbiamo aggiunto la proprietà `Position` alla classe `ContentField`:
```csharp
public class ContentField {
    public string Name { get; set; }
    public int Position { get; set; } // <--- Nuova proprietà
    // ... altri campi
}
```

### 2. Interfaccia Amministrativa (EditContentType.razor)
L'editor dei tipi di contenuto ora include:
- **Visualizzazione Posizione**: Una nuova colonna mostra la posizione attuale (1-based).
- **Controlli di Ordinamento**: Pulsanti "Su" e "Giù" per spostare i campi.
- **Logica di Riepilogo**: All'aggiunta di un nuovo campo, gli viene assegnata l'ultima posizione disponibile. Alla cancellazione, gli indici rimanenti vengono ricalcolati per non avere "buchi".

### 3. Dinamismo UI (EditContentItem.razor)
Quando si crea o si modifica un contenuto:
- Il sistema recupera i campi dal Content Type.
- Esegue un ordinamento `OrderBy(f => f.Position)` prima di renderizzare il form.
- Questo garantisce che gli editor vedano i campi nell'ordine esatto deciso in fase di design del tipo di contenuto.

## Osservabilità e Telemetria

Seguendo gli standard di **Pollon.ServiceDefaults**, abbiamo arricchito l'osservabilità di questa funzione:

- **Structured Logging nel Backoffice Web**: Ogni volta che un campo viene spostato nell'interfaccia, viene generato un log informativo che identifica il campo, la vecchia e la nuova posizione.
- **Audit Log nel Backoffice API**: Gli endpoint `PUT` (Update) e `POST` (Create) dei Content Type ora emettono un log strutturato che elenca la sequenza finale dei campi salvati.
- **Integrazione Tracing**: Grazie all'uso di `ILogger` integrato con OpenTelemetry, questi log sono correlati agli spans delle chiamate API e sono visibili nella Dashboard di Aspire e in Jaeger sotto lo stesso `TraceId`.

## Verifica Tecnica
- [x] Salvataggio della posizione su PostgreSQL via Marten.
- [x] Ordinamento reattivo nell'interfaccia Blazor.
- [x] Ordinamento dei campi nel form di editing del contenuto.
- [x] Emissione log strutturati per telemetria.
