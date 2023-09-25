using ProductNotifier;
using ProductNotifier.Config;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddOptions<MonitorConfiguration>()
            .BindConfiguration(MonitorConfiguration.SectionKey)
            .ValidateOnStart();
        
        services.AddOptions<DiscordConfiguration>()
            .BindConfiguration(DiscordConfiguration.SectionKey)
            .ValidateOnStart();
        
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();