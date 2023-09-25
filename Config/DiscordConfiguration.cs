using System.ComponentModel.DataAnnotations;

namespace ProductNotifier.Config;

public class DiscordConfiguration
{
    public const string SectionKey = "Discord";
    
    [Required]
    public string WebhookUrl { get; set; }
}