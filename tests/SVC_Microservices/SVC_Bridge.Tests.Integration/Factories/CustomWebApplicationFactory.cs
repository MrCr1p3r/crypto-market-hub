using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.DataCollectors.Interfaces;

namespace SVC_Bridge.Tests.Integration.Factories
{
    public class CustomWebApplicationFactory(
        IKlineDataCollector dataCollector,
        ISvcKlineClient klineClient
    ) : WebApplicationFactory<Program>
    {
        private readonly IKlineDataCollector _dataCollector = dataCollector;
        private readonly ISvcKlineClient _klineClient = klineClient;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing IKlineDataCollector registration
                var dataCollectorDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(IKlineDataCollector)
                );
                if (dataCollectorDescriptor != null)
                {
                    services.Remove(dataCollectorDescriptor);
                }

                // Remove the existing ISvcKlineClient registration
                var klineClientDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(ISvcKlineClient)
                );
                if (klineClientDescriptor != null)
                {
                    services.Remove(klineClientDescriptor);
                }

                // Register the mocked IKlineDataCollector and ISvcKlineClient
                services.AddSingleton(_dataCollector);
                services.AddSingleton(_klineClient);
            });
        }
    }
}
