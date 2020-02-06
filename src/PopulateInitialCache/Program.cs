using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasPublicDownload;
using RestSharp;

namespace PopulateInitialCache
{
    class Program
    {
        private static Logger _logger;
        private static IGiasApiClient _giasApiClient;
        private static IEstablishmentRepository _establishmentRepository;

        static async Task Run(CommandLineOptions options, CancellationToken cancellationToken = default)
        {
            Init(options);

            var establishments = await GetEstablishments(cancellationToken);
            await StoreEstablishments(establishments, cancellationToken);
        }

        static void Init(CommandLineOptions options)
        {
            _giasApiClient = new GiasPublicDownloadClient(new RestClient(), _logger);

            _establishmentRepository = new TableEstablishmentRepository(new CacheConfiguration
            {
                TableStorageConnectionString = options.StorageConnectionString,
                EstablishmentTableName = options.EstablishmentsTableName,
            }, _logger);
        }

        static async Task<Establishment[]> GetEstablishments(CancellationToken cancellationToken)
        {
            _logger.Info("Downloading establishments...");
            var establishments = await _giasApiClient.DownloadEstablishmentsAsync(cancellationToken);
            _logger.Info($"Downloaded {establishments.Length} establishments");
            return establishments;
        }

        static async Task StoreEstablishments(Establishment[] establishments, CancellationToken cancellationToken)
        {
            for (var i = 0; i < establishments.Length; i++)
            {
                _logger.Info($"Storing establishment {i} of {establishments.Length}: {establishments[i].Urn}");

                await _establishmentRepository.StoreAsync(establishments[i], cancellationToken);
            }
        }
        
        
        static void Main(string[] args)
        {
            _logger = new Logger();

            CommandLineOptions options = null;
            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed((parsed) => options = parsed);
            if (options != null)
            {
                try
                {
                    Run(options).Wait();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }

                _logger.Info("Done. Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}