using System.ComponentModel.DataAnnotations;

namespace ProductNotifier.Config;

public class MonitorConfiguration
{
    public const string SectionKey = "Monitor";

    [Required]
    public string Url { get; set; } = "";
    [Required]
    public string XPath { get; set; }
    [Required, MinLength(2)]
    public int[] BetweenDelayMsOutOfStock { get; set; }
    [Required, MinLength(2)]
    public int[] BetweenDelayMsInStock { get; set; }
}