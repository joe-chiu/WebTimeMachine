using System.Text.Json.Serialization;

class WaybackProxyConfig
{
    [JsonPropertyName("DATE")]
    public string Date { get; set; } = "";
}