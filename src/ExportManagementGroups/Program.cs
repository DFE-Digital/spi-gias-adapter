using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.Common.Caching;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi;
using Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping;
using Dfe.Spi.GiasAdapter.Infrastructure.SpiTranslator;
using Dfe.Spi.Models.Entities;
using Newtonsoft.Json;
using RestSharp;

namespace ExportManagementGroups
{
    class Program
    {
        private static Logger _logger;
        private static HttpSpiExecutionContextManager _httpSpiExecutionContextManager;
        private static IGiasApiClient _giasApiClient;
        private static IMapper _mapper;

        static async Task Run(CommandLineOptions options, CancellationToken cancellationToken = default)
        {
            Init(options);

            var localAuthorities = await GetLocalAuthorities(cancellationToken);
            var groups = await GetGroups(cancellationToken);

            var managementGroups = localAuthorities.Concat(groups).ToArray();
            await WriteOutput(managementGroups, options.OutputPath, cancellationToken);
        }

        static void Init(CommandLineOptions options)
        {
            _giasApiClient = new GiasSoapApiClient(new GiasApiConfiguration
            {
                Url = options.GiasUrl,
                Username = options.GiasUsername,
                Password = options.GiasPassword,
                ExtractId = options.GiasExtractId,
                ExtractEstablishmentsFileName = "Eapim_Daily_Download.csv",
                ExtractGroupsFileName = "groups.csv",
                ExtractGroupLinksFileName = "groupLinks.csv",
            });

            _httpSpiExecutionContextManager = new HttpSpiExecutionContextManager();
            _httpSpiExecutionContextManager.SetInternalRequestId(Guid.NewGuid());

            var translator = new TranslatorApiClient(
                new AuthenticationConfiguration()
                {
                    ClientId = options.ClientId,
                    ClientSecret = options.ClientSecret,
                    Resource = options.Resource,
                    TokenEndpoint = options.TokenEndpoint,
                },
                new RestClient(),
                new CacheProvider(),
                _httpSpiExecutionContextManager,
                new TranslatorConfiguration
                {
                    BaseUrl = options.TranslatorBaseUrl,
                    SubscriptionKey = options.TranslatorSubscriptionKey,
                }, 
                _logger);
            _mapper = new PocoMapper(translator, null, null);
        }

        static async Task<ManagementGroup[]> GetLocalAuthorities(CancellationToken cancellationToken)
        {
            _logger.Info("Downloading establishments...");
            var establishments = await _giasApiClient.DownloadEstablishmentsAsync(cancellationToken);
            
            var localAuthorities = establishments
                .Select(e => e.LA)
                .GroupBy(la => la.Code)
                .Select(la => la.First())
                .Select(cnp => 
                    new LocalAuthority
                    {
                        Code = int.Parse(cnp.Code),
                        Name = cnp.DisplayName,
                    })
                .ToArray();
            _logger.Debug($"Converted {establishments.Length} to {localAuthorities.Length} distinct local authorities");

            return await MapAsync(localAuthorities, cancellationToken);
        }

        static async Task<ManagementGroup[]> GetGroups(CancellationToken cancellationToken)
        {
            _logger.Info("Downloading groups...");
            var groups = await _giasApiClient.DownloadGroupsAsync(cancellationToken);

            return await MapAsync(groups, cancellationToken);
        }

        static async Task<ManagementGroup[]> MapAsync<T>(T[] items, CancellationToken cancellationToken)
        {
            var managementGroups = new ManagementGroup[items.Length];
            
            for (var i = 0; i < items.Length; i++)
            {
                managementGroups[i] = await _mapper.MapAsync<ManagementGroup>(items[i], cancellationToken);
                managementGroups[i]._Lineage = null;
            }

            return managementGroups;
        }

        static async Task WriteOutput(ManagementGroup[] managementGroups, string path, CancellationToken cancellationToken)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                _logger.Info($"Creating directory {dir}");
                Directory.CreateDirectory(dir);
            }
            
            var json = JsonConvert.SerializeObject(managementGroups);
            using(var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(json);
                await writer.FlushAsync();
            }
            
            _logger.Info($"Written {managementGroups.Length} management groups to {path}");
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
            }
        }
    }
}