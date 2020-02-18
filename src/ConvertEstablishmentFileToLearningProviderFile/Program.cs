using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasPublicDownload;
using Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping;
using Dfe.Spi.GiasAdapter.Infrastructure.SpiTranslator;
using Dfe.Spi.Models.Entities;
using Newtonsoft.Json;
using RestSharp;

namespace ConvertEstablishmentFileToLearningProviderFile
{
    class Program
    {
        private static Logger _logger;
        private static IGiasApiClient _giasApiClient;
        private static IMapper _mapper;

        static async Task Run(CommandLineOptions options, CancellationToken cancellationToken = default)
        {
            Init(options);

            var establishments = await GetEstablishments(cancellationToken);
            var learningProviders = await MapEstablishmentsToLearningProviders(establishments, cancellationToken);
            await WriteOutput(learningProviders, options.OutputPath, cancellationToken);
        }

        static void Init(CommandLineOptions options)
        {
            _giasApiClient = new GiasPublicDownloadClient(new RestClient(), _logger);

            var translator = new TranslatorApiClient(
                new RestClient(),
                new TranslatorConfiguration
                {
                    BaseUrl = options.TranslatorBaseUrl,
                    FunctionsKey = options.TranslatorFunctionKey,
                }, 
                _logger);
            _mapper = new PocoMapper(translator);
        }

        static async Task<Establishment[]> GetEstablishments(CancellationToken cancellationToken)
        {
            _logger.Info("Downloading establishments...");
            var establishments = await _giasApiClient.DownloadEstablishmentsAsync(cancellationToken);
            _logger.Info($"Downloaded {establishments.Length} establishments");
            return establishments;
        }

        static async Task<LearningProvider[]> MapEstablishmentsToLearningProviders(Establishment[] establishments, CancellationToken cancellationToken)
        {
            var learningProviders = new LearningProvider[establishments.Length];
            
            for (var i = 0; i < establishments.Length; i++)
            {
                _logger.Info($"Starting to map establishment {i} of {establishments.Length}");

                learningProviders[i] = await _mapper.MapAsync<LearningProvider>(establishments[i], cancellationToken);
            }
            
            _logger.Info($"Mapped {learningProviders.Length} learning providers");
            return learningProviders;
        }

        static async Task WriteOutput(LearningProvider[] learningProviders, string path, CancellationToken cancellationToken)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                _logger.Info($"Creating directory {dir}");
                Directory.CreateDirectory(dir);
            }
            
            var json = JsonConvert.SerializeObject(learningProviders);
            using(var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(json);
                await writer.FlushAsync();
            }
            
            _logger.Info($"Written {learningProviders.Length} learning providers to {path}");
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