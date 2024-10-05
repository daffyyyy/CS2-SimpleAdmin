using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CS2_SimpleAdmin.Managers;

public class DiscordManager(string webhookUrl)
{
    public async Task SendMessageAsync(string message)
    {
        var client = CS2_SimpleAdmin.HttpClient;
        
        var payload = new
        {
            content = message
        };

        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(webhookUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                CS2_SimpleAdmin._logger?.LogError(
                    $"Failed to send discord message. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
            }
        }
        catch (HttpRequestException e)
        {
            CS2_SimpleAdmin._logger?.LogError($"Error sending discord message: {e.Message}");
        }
    }

    public async Task SendEmbedAsync(Embed embed)
    {
        var httpClient = CS2_SimpleAdmin.HttpClient;

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    color = embed.Color,
                    title = !string.IsNullOrEmpty(embed.Title) ? embed.Title : null,
                    description = !string.IsNullOrEmpty(embed.Description) ? embed.Description : null,
                    thumbnail = !string.IsNullOrEmpty(embed.ThumbnailUrl) ? new { url = embed.ThumbnailUrl } : null,
                    image = !string.IsNullOrEmpty(embed.ImageUrl) ? new { url = embed.ImageUrl } : null,
                    footer = !string.IsNullOrEmpty(embed.Footer?.Text) ? new { text = embed.Footer.Text, icon_url = embed.Footer.IconUrl } : null,
                    timestamp = embed.Timestamp,
                    fields = embed.Fields.Count > 0 ? embed.Fields.Select(field => new
                    {
                        name = field.Name,
                        value = field.Value,
                        inline = field.Inline
                    }).ToArray() : null
                }
            }
        };

        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(webhookUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            CS2_SimpleAdmin._logger?.LogError($"Failed to send embed: {response.StatusCode} - {errorMessage}");
        }
    }
    
    public static int ColorFromHex(string hex)
    {
        if (hex.StartsWith($"#"))
        {
            hex = hex[1..];
        }

        return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
    }
}

public class Embed
{
    public int Color { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public Footer? Footer { get; init; }
    public string? Timestamp { get; init; }

    public List<EmbedField> Fields { get; } = [];

    public void AddField(string name, string value, bool inline)
    {
        var field = new EmbedField
        {
            Name = name,
            Value = value,
            Inline = inline
        };
        
        Fields.Add(field);
    }
}

public class Footer
{
    public string? Text { get; init; }
    public string? IconUrl { get; set; }
}

public class EmbedField
{
    public string? Name { get; init; }
    public string? Value { get; init; }
    public bool Inline { get; init; }
}

