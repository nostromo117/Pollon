namespace Pollon.Publication.Models;

public class ContentTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string PreviewImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Sorgente Scriban inline. Se valorizzato, ha precedenza sul file .sbn su disco.
    /// Permette di creare e modificare template direttamente dal backoffice.
    /// </summary>
    public string? TemplateContent { get; set; }

    /// <summary>
    /// Variabili chiave/valore iniettate nel contesto Scriban durante il rendering.
    /// Usabili nel template come: {{ vars.primary_color }}, {{ vars.font_family }}, ecc.
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// Se false il template non compare nel dropdown di selezione dei Content Types.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public List<string> Tags { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
