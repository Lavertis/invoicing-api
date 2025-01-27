using System.Text.Json.Serialization;

namespace Invoicing.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceOperationType
{
    Start,
    Suspend,
    Resume,
    End
}