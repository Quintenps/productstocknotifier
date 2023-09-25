using Discord.Webhook;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using ProductNotifier.Config;

namespace ProductNotifier;

public class Worker : BackgroundService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Worker> _logger;
    private readonly MonitorConfiguration _monitorConfiguration;
    private readonly DiscordWebhookClient _discordWebhookClient;

    private bool _isMentionedInStock;
    private const string ErrorWebpage = "<@140276297187196928> I failed to scrape the webpage, wtf?";
    private const string InStockMessage = "<@140276297187196928> GET THE CREDITCARD 🔔️";
    
    public Worker(ILogger<Worker> logger, IOptions<MonitorConfiguration> monitorConfig, IOptions<DiscordConfiguration> discordConfig)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _monitorConfiguration = monitorConfig.Value ?? throw new ArgumentNullException(nameof(monitorConfig));
     
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36 OPR/38.0.2220.41");
        
        var discordConfiguration = discordConfig.Value ?? throw new ArgumentNullException(nameof(discordConfig));
        _discordWebhookClient = new DiscordWebhookClient(discordConfiguration.WebhookUrl);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            
            var htmlContent = await GetWebpage();
            if (htmlContent is null)
            {
                await _discordWebhookClient.SendMessageAsync(ErrorWebpage);
                continue;
            }
            
            var purchaseButtons = ExtractHtmlElements(htmlContent);
            var inStock = DetectIfInStock(purchaseButtons);
            
            if (inStock)
            {
                _logger.LogInformation("In stock");
                _isMentionedInStock = true;
                await _discordWebhookClient.SendMessageAsync(InStockMessage);
            }
            else
            {
                _logger.LogInformation("Out of stock");
                _isMentionedInStock = false;
            }
            
            var random = new Random();
            var waitingDelay = _isMentionedInStock
                ? random.Next(_monitorConfiguration.BetweenDelayMsInStock[0], _monitorConfiguration.BetweenDelayMsInStock[1])
                : random.Next(_monitorConfiguration.BetweenDelayMsOutOfStock[0], _monitorConfiguration.BetweenDelayMsOutOfStock[1]);
            var nextCheckTime = TimeSpan.FromMilliseconds(waitingDelay).ToString(@"hh\:mm\:ss");
            
            _logger.LogInformation("Next check @ {CheckTime}", nextCheckTime);
            await Task.Delay(waitingDelay, stoppingToken);
        }
    }

    private static bool DetectIfInStock(HtmlNodeCollection? purchaseButtons)
    {
        if (purchaseButtons == null || !purchaseButtons.Any())
        {
            return false;
        }

        return !purchaseButtons.All(b => b.Attributes.Any(a => a.Name == "aria-label"));
    }

    private async Task<string?> GetWebpage()
    {
        var response = await _httpClient.GetAsync(_monitorConfiguration.Url);
        if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync();
        
        _logger.LogError("Failed to fetch webpage - statuscode {StatusCode}", response.StatusCode);
        return null;
    }

    private HtmlNodeCollection? ExtractHtmlElements(string htmlDom)
    {
        var htmlSnippet = new HtmlDocument();
        htmlSnippet.LoadHtml(htmlDom);
        
        return htmlSnippet.DocumentNode.SelectNodes(_monitorConfiguration.XPath);
    }
}