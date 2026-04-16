using System.Text.Json.Serialization;

namespace Pollon.Publication.Models;

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
