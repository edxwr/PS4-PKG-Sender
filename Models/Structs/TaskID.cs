using System.Text.Json.Serialization;

namespace Ps4PkgSender.Models.Structs;

public record TaskID
{
    [JsonPropertyName("task_id")]
    public int Id {get; set;}
}