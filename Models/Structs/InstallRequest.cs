using System;
using System.Text.Json.Serialization;

namespace Ps4PkgSender.Models.Structs;

public record InstallRequest
{
    [JsonPropertyName("action")]
    public string Action { get; } = "install";

    [JsonPropertyName("url")]
    public string Url { get; set; } = String.Empty;
}