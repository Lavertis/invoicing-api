using System.Text.Json.Serialization;

namespace Invoicing.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationType
{
    StartService,
    SuspendService,
    ResumeService,
    EndService
}