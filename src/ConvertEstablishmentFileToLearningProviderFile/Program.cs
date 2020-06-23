using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.Common.Caching;
using Dfe.Spi.Common.Context;
using Dfe.Spi.Common.Context.Definitions;
using Dfe.Spi.Common.Context.Models;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasPublicDownload;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi;
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
        private static HttpSpiExecutionContextManager _httpSpiExecutionContextManager;
        private static IGiasApiClient _giasApiClient;
        private static InProcGroupRepository _groupRepository;
        private static InProcLocalAuthorityRepository _localAuthorityRepository;
        private static IMapper _mapper;

        static async Task Run(CommandLineOptions options, CancellationToken cancellationToken = default)
        {
            Init(options);

            var establishments = await GetEstablishments(cancellationToken);
            var groups = await GetGroups(cancellationToken);
            var groupLinks = await GetGroupLinks(cancellationToken);

            SetRepositoryData(establishments, groups);
            UpdateEstablishmentsWithLinks(establishments, groupLinks);

            var learningProviders = await MapEstablishmentsToLearningProviders(establishments, cancellationToken);
            await WriteOutput(learningProviders, options.OutputPath, cancellationToken);
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

            _groupRepository = new InProcGroupRepository();

            _localAuthorityRepository = new InProcLocalAuthorityRepository();

            _mapper = new PocoMapper(translator, _groupRepository, _localAuthorityRepository);
        }

        static async Task<Establishment[]> GetEstablishments(CancellationToken cancellationToken)
        {
            _logger.Info("Downloading establishments...");
            var establishments = await _giasApiClient.DownloadEstablishmentsAsync(cancellationToken);
            _logger.Info($"Downloaded {establishments.Length} establishments");
            return establishments;
        }

        static async Task<PointInTimeGroup[]> GetGroups(CancellationToken cancellationToken)
        {
            _logger.Info("Downloading groups...");
            var groups = await _giasApiClient.DownloadGroupsAsync(cancellationToken);
            _logger.Info($"Downloaded {groups.Length} groups");

            var pointInTimeGroups = groups.Select(group => Clone<PointInTimeGroup>(group)).ToArray();
            foreach (var pointInTimeGroup in pointInTimeGroups)
            {
                pointInTimeGroup.PointInTime = DateTime.UtcNow.Date;
            }

            return pointInTimeGroups;
        }

        static async Task<GroupLink[]> GetGroupLinks(CancellationToken cancellationToken)
        {
            _logger.Info("Downloading group links...");
            var groupLinks = await _giasApiClient.DownloadGroupLinksAsync(cancellationToken);
            _logger.Info($"Downloaded {groupLinks.Length} group links");
            return groupLinks;
        }

        static void SetRepositoryData(Establishment[] establishments, PointInTimeGroup[] groups)
        {
            _groupRepository.SetData(groups);

            var localAuthorities = establishments
                .Where(e => e.LA != null)
                .Select(e => new PointInTimeLocalAuthority {Code = int.Parse(e.LA.Code), Name = e.LA.DisplayName, PointInTime = DateTime.UtcNow.Date})
                .GroupBy(la => la.Code)
                .Select(grp => grp.First())
                .ToArray();
            _localAuthorityRepository.SetData(localAuthorities);
        }

        static void UpdateEstablishmentsWithLinks(Establishment[] establishments, GroupLink[] groupLinks)
        {
            foreach (var establishment in establishments)
            {
                var establishmentGroupLinks = groupLinks.Where(l => l.Urn == establishment.Urn).ToArray();
                var federationLink = establishmentGroupLinks.FirstOrDefault(l => l.GroupType == "Federation");
                var trustLink = establishmentGroupLinks.FirstOrDefault(l =>
                    l.GroupType == "Trust" || l.GroupType == "Single-academy trust" ||
                    l.GroupType == "Multi-academy trust");

                if (federationLink != null)
                {
                    establishment.Federations = new CodeNamePair
                    {
                        Code = federationLink.Uid.ToString(),
                    };
                    _logger.Debug(
                        $"Set Federations to {federationLink.Uid} from links of establishment {establishment.Urn}");
                }

                if (trustLink != null)
                {
                    establishment.Trusts = new CodeNamePair
                    {
                        Code = trustLink.Uid.ToString(),
                    };
                    _logger.Debug($"Set Trusts to {trustLink.Uid} from links of establishment {establishment.Urn}");
                }
            }
        }

        static async Task<LearningProvider[]> MapEstablishmentsToLearningProviders(Establishment[] establishments,
            CancellationToken cancellationToken)
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

        static async Task WriteOutput(LearningProvider[] learningProviders, string path,
            CancellationToken cancellationToken)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                _logger.Info($"Creating directory {dir}");
                Directory.CreateDirectory(dir);
            }

            var json = JsonConvert.SerializeObject(learningProviders);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(json);
                await writer.FlushAsync();
            }

            _logger.Info($"Written {learningProviders.Length} learning providers to {path}");
        }


        static TDestination Clone<TDestination>(object source, Func<TDestination> activator = null)
        {
            // TODO: This could be more efficient with some caching of properties
            var sourceProperties = source.GetType().GetProperties();
            var destinationProperties = source.GetType().GetProperties();

            TDestination destination;
            if (activator != null)
            {
                destination = activator();
            }
            else
            {
                destination = Activator.CreateInstance<TDestination>();
            }

            foreach (var destinationProperty in destinationProperties)
            {
                var sourceProperty = sourceProperties.SingleOrDefault(p => p.Name == destinationProperty.Name);
                if (sourceProperty != null)
                {
                    // TODO: This assumes the property types are the same. If this is not true then handling will be required
                    var sourceValue = sourceProperty.GetValue(source);
                    destinationProperty.SetValue(destination, sourceValue);
                }
            }

            return destination;
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


    class InProcGroupRepository : IGroupRepository
    {
        private PointInTimeGroup[] _groups;

        public void SetData(PointInTimeGroup[] groups)
        {
            _groups = groups;
        }

        public Task StoreAsync(PointInTimeGroup @group, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task StoreAsync(PointInTimeGroup[] groups, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task StoreInStagingAsync(PointInTimeGroup[] groups, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<PointInTimeGroup> GetGroupAsync(long uid, CancellationToken cancellationToken)
        {
            if (_groups == null)
            {
                throw new Exception("Must SetData first");
            }

            var group = _groups.SingleOrDefault(g => g.Uid == uid);
            return Task.FromResult(group);
        }

        public Task<PointInTimeGroup> GetGroupFromStagingAsync(long uid, DateTime pointInTime, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    class InProcLocalAuthorityRepository : ILocalAuthorityRepository
    {
        private PointInTimeLocalAuthority[] _localAuthorities;

        public void SetData(PointInTimeLocalAuthority[] localAuthorities)
        {
            _localAuthorities = localAuthorities;
        }

        public Task StoreAsync(PointInTimeLocalAuthority localAuthority, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task StoreAsync(PointInTimeLocalAuthority[] localAuthorities, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StoreInStagingAsync(PointInTimeLocalAuthority[] localAuthorities, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<PointInTimeLocalAuthority> GetLocalAuthorityAsync(int laCode, CancellationToken cancellationToken)
        {
            if (_localAuthorities == null)
            {
                throw new Exception("Must SetData first");
            }

            var localAuthority = _localAuthorities.SingleOrDefault(g => g.Code == laCode);
            return Task.FromResult(localAuthority);
        }

        public Task<PointInTimeLocalAuthority> GetLocalAuthorityFromStagingAsync(int laCode, DateTime pointInTime, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}