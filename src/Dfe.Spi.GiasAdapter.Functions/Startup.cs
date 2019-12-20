using System.IO;
using Dfe.Spi.Common.Logging;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Application.Cache;
using Dfe.Spi.GiasAdapter.Application.LearningProviders;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.Events;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.GiasAdapter.Functions;
using Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasPublicDownload;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi;
using Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping;
using Dfe.Spi.GiasAdapter.Infrastructure.SpiMiddleware;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestSharp;

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
            AddHttp(services);
            AddEventPublishing(services);
            AddRepositories(services);
            AddGiasApi(services);
            AddMapping(services);
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
            services.AddSingleton(_configuration.GiasApi);
            services.AddSingleton(_configuration.Cache);
        }

        private void AddLogging(IServiceCollection services)
        {
            services.AddLogging();
            services.AddScoped(typeof(ILogger<>), typeof(Logger<>));
            services.AddScoped<ILogger>(provider =>
                provider.GetService<ILoggerFactory>().CreateLogger(LogCategories.CreateFunctionUserCategory("Common")));
            services.AddScoped<ILoggerWrapper, LoggerWrapper>();
        }

        private void AddHttp(IServiceCollection services)
        {
            services.AddScoped<IRestClient, RestClient>();
        }

        private void AddEventPublishing(IServiceCollection services)
        {
            services.AddScoped<IEventPublisher, MiddlewareEventPublisher>();
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddScoped<IEstablishmentRepository, TableEstablishmentRepository>();
        }

        private void AddGiasApi(IServiceCollection services)
        {
            services.AddScoped<IGiasApiClient, GiasSoapApiClient>();
        }

        private void AddMapping(IServiceCollection services)
        {
            services.AddScoped<IMapper, PocoMapper>();
        }

        private void AddManagers(IServiceCollection services)
        {
            services.AddScoped<ILearningProviderManager, LearningProviderManager>();
            services.AddScoped<ICacheManager>((sp) =>
            {
                // TODO: Once we have SOAP extracts, this can be a "standard" registration
                //       Currently have 2 IGiasApiClient implementations
                var logger = sp.GetService<ILoggerWrapper>();
                var apiClient = new GiasPublicDownloadClient(sp.GetService<IRestClient>(), logger);
                var establishmentRepository = sp.GetService<IEstablishmentRepository>();
                var mapper = sp.GetService<IMapper>();
                var eventPublisher = sp.GetService<IEventPublisher>();
                var establishmentProcessingQueue = sp.GetService<IEstablishmentProcessingQueue>();
                return new CacheManager(apiClient, establishmentRepository, mapper, eventPublisher, establishmentProcessingQueue, logger);
            });
        }
    }
}