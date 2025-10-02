using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin.Managers;

public class DiscordManager(string webhookUrl)
{
    
    /// <summary>
    /// Sends a plain text message asynchronously to the configured Discord webhook URL.
    /// </summary>
    /// <param name="message">The text message to send to Discord.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public async Task SendMessageAsync(string message)
    {
        var client = CS2_SimpleAdmin.HttpClient;
        var payload = new
        {
            content = message
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(payload, options);
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

    /// <summary>
    /// Sends an embed message asynchronously to the configured Discord webhook URL.
    /// </summary>
    /// <param name="embed">The embed object containing rich content to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
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

        var options = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        var jsonPayload = JsonSerializer.Serialize(payload, options);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(webhookUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            CS2_SimpleAdmin._logger?.LogError($"Failed to send embed: {response.StatusCode} - {errorMessage}");
        }
    }
    
    /// <summary>
    /// Converts a hexadecimal color string (e.g. "#FF0000") to its integer representation.
    /// </summary>
    /// <param name="hex">The hexadecimal color string, optionally starting with '#'.</param>
    /// <returns>An integer representing the color.</returns>
    public static int ColorFromHex(string hex)
    {
        if (hex.StartsWith($"#"))
        {
            hex = hex[1..];
        }

        return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
    }
}

/// <summary>
/// Represents a Discord embed message containing rich content such as title, description, fields, and images.
/// </summary>
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

    /// <summary>
    /// Adds a field to the embed message.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="value">The value or content of the field.</param>
    /// <param name="inline">Whether the field should be displayed inline with other fields.</param>
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

/// <summary>
/// Represents the footer section of a Discord embed message, including optional text and icon URL.
/// </summary>
public class Footer
{
    public string? Text { get; init; }
    public string? IconUrl { get; set; }
}

/// <summary>
/// Represents a field inside a Discord embed message.
/// </summary>
public class EmbedField
{
    public string? Name { get; init; }
    public string? Value { get; init; }
    public bool Inline { get; init; }
}

