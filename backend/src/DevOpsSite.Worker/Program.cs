using DevOpsSite.Worker.Composition;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddWorkerServices(hostContext.Configuration);
    });

var host = builder.Build();

// Startup validation — Constitution §13.5: fail-fast on invalid state
host.Services.ValidateWorkerServices();

await host.RunAsync();
