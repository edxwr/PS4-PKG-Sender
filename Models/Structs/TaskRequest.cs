using System.Text.Json.Serialization;

namespace Ps4PkgSender.Models.Structs;

public record TaskRequest
{
    [JsonPropertyName("content_id")]
    public string ContentID {get; set;} = "";

    [JsonPropertyName("sub_type")]
    public int Subtype {get; set;} = 6;
}