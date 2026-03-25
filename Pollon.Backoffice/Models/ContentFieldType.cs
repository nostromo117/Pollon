using System.Text.Json.Serialization;

namespace Pollon.Backoffice.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentFieldType
{
    Text,
    RichText,
    Number,
    Boolean,
    Date,
    Image
}
