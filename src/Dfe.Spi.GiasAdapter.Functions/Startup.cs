using System.IO;
using Dfe.Spi.GiasAdapter.Application.LearningProviders;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Functions;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Dfe.Spi.GiasAdapter.Functions
{
    public class Startup : FunctionsStartup
    {
        private IConfigurationRoot _rawConfiguration;
        private GiasAdapterConfiguration _configuration;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var services = builder.Services;

            LoadAndAddConfiguration(services);
            AddLogging(services);
            AddGiasApi(services);
            AddManagers(services);
        }

        private void LoadAndAddConfiguration(IServiceCollection services)
        {
            _rawConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", true)
                .AddEnvironmentVariables(prefix: "SPI_")
                .Build();
            services.AddSingleton(_rawConfiguration);

            _configuration = new GiasAdapterConfiguration();
            _rawConfiguration.Bind(_configuration);
            services.AddSingleton(_configuration);
        }

        private void AddLogging(IServiceCollection services)
        {
            services.AddLogging();
            services.AddScoped(typeof(ILogger<>), typeof(Logger<>));
            services.AddScoped<ILogger>(provider =>
                provider.GetService<ILoggerFactory>().CreateLogger(LogCategories.CreateFunctionUserCategory("Common")));
        }

        private void AddGiasApi(IServiceCollection services)
        {
            services.AddScoped<IGiasApiClient, GiasSoapApiClient>();
        }

        private void AddManagers(IServiceCollection services)
        {
            services.AddScoped<ILearningProviderManager, LearningProviderManager>();
        }
    }
}