using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SVC_External.DataCollectors.Interfaces;

namespace SVC_External.Tests.Integration.Factories;

public class CustomWebApplicationFactory(IExchangesDataCollector dataCollector) 
    : WebApplicationFactory<Program>
{
    private readonly IExchangesDataCollector _dataCollector = dataCollector;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing IExchangesDataCollector registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IExchangesDataCollector));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Register the mock IExchangesDataCollector
            services.AddSingleton(_dataCollector);
        });
    }
}
