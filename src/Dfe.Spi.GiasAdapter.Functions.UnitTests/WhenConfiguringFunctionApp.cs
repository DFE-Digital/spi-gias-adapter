using System;
using System.Collections.Generic;
using Dfe.Spi.GiasAdapter.Functions.Cache;
using Dfe.Spi.GiasAdapter.Functions.LearningProviders;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Functions.UnitTests
{
    public class WhenConfiguringFunctionApp
    {
        [Test]
        public void ThenAllFunctionsShouldBeResolvable()
        {
            var functions = GetFunctions();
            var builder = new TestFunctionHostBuilder();
            var configuration = GetTestConfiguration();
            
            var startup = new Startup();
            startup.Configure(builder, configuration);
            // Have to register the function so container can attempt to resolve them
            foreach (var function in functions)
            {
                builder.Services.AddScoped(function);
            }
            var provider = builder.Services.BuildServiceProvider();
            
            foreach (var function in functions)
            {
                Assert.IsNotNull(provider.GetService(function),
                    $"Failed to resolve {function.Name}");
            }
        }

        private IConfigurationRoot GetTestConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("GiasApi:Url", "https://gias.unit.tests"),
                    new KeyValuePair<string, string>("GiasApi:Username", "unit_tests"),
                    new KeyValuePair<string, string>("GiasApi:Password", "some-secure-password"),
                    new KeyValuePair<string, string>("Cache:TableStorageConnectionString", "UseDevelopmentStorage=true"),
                    new KeyValuePair<string, string>("Cache:EstablishmentTableName", "unitests"),
                    new KeyValuePair<string, string>("Cache:EstablishmentProcessingQueueConnectionString", "UseDevelopmentStorage=true"),
                    new KeyValuePair<string, string>("Cache:EstablishmentSchedule", "0 0 5 * * *"),
                }).Build();
        }

        private Type[] GetFunctions()
        {
            return new[]
            {
                typeof(ProcessBatchOfEstablishments),
                typeof(DownloadEstablishmentsScheduled),
                typeof(GetLearningProvider),
            };
        }
        
        private class TestFunctionHostBuilder : IFunctionsHostBuilder
        {
            public TestFunctionHostBuilder()
            {
                Services = new ServiceCollection();
            }
            public IServiceCollection Services { get; }
        }
    }
}