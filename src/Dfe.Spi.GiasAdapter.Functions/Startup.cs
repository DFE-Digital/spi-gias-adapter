using System.IO;
using Dfe.Spi.Common.Context.Definitions;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Application.Cache;
using Dfe.Spi.GiasAdapter.Application.LearningProviders;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.Events;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.GiasAdapter.Domain.Translation;
using Dfe.Spi.GiasAdapter.Functions;
using Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasPublicDownload;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi;
using Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping;
using Dfe.Spi.GiasAdapter.Infrastructure.SpiMiddleware;
using Dfe.Spi.GiasAdapter.Infrastructure.SpiTranslator;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
            var rawConfiguration = BuildConfiguration();
            Configure(builder, rawConfiguration);
        }

        public void Configure(IFunctionsHostBuilder builder, IConfigurationRoot rawConfiguration)
        {
            var services = builder.Services;
            
            JsonConvert.DefaultSettings =
                () => new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore,
                };

            AddConfiguration(services, rawConfiguration);
            AddLogging(services);
            AddHttp(services);
            AddEventPublishing(services);
            AddTranslation(services);
            AddRepositories(services);
            AddQueues(services);
            AddGiasApi(services);
            AddMapping(services);
            AddManagers(services);
        }

        private IConfigurationRoot BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", true)
                .AddEnvironmentVariables(prefix: "SPI_")
                .Build();
        }

        private void AddConfiguration(IServiceCollection services, IConfigurationRoot rawConfiguration)
        {
            _rawConfiguration = rawConfiguration;
            services.AddSingleton(_rawConfiguration);
            
            _configuration = new GiasAdapterConfiguration();
            _rawConfiguration.Bind(_configuration);
            services.AddSingleton(_configuration);
            services.AddSingleton(_configuration.GiasApi);
            services.AddSingleton(_configuration.Cache);
            services.AddSingleton(_configuration.Middleware);
            services.AddSingleton(_configuration.Translator);
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
            services.AddTransient<IRestClient, RestClient>();
        }

        private void AddEventPublishing(IServiceCollection services)
        {
            services.AddScoped<IEventPublisher, MiddlewareEventPublisher>();
        }

        private void AddTranslation(IServiceCollection services)
        {
            services.AddScoped<ITranslator, TranslatorApiClient>();
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddScoped<IEstablishmentRepository, TableEstablishmentRepository>();
            services.AddScoped<IGroupRepository, TableGroupRepository>();
            services.AddScoped<ILocalAuthorityRepository, TableLocalAuthorityRepository>();
        }

        private void AddQueues(IServiceCollection services)
        {
            services.AddScoped<IEstablishmentProcessingQueue, QueueEstablishmentProcessingQueue>();
            services.AddScoped<IGroupProcessingQueue, QueueGroupProcessingQueue>();
            services.AddScoped<ILocalAuthorityProcessingQueue, QueueLocalAuthorityProcessingQueue>();
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
            services.AddScoped<ICacheManager, CacheManager>();
            services.AddScoped<IHttpSpiExecutionContextManager, HttpSpiExecutionContextManager>();
            services.AddScoped<ISpiExecutionContextManager>(x => x.GetService<IHttpSpiExecutionContextManager>());
        }
    }
}