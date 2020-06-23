using System;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasPublicDownload;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi;
using RestSharp;

namespace PopulateInitialCache
{
    class Program
    {
        private static Logger _logger;
        private static IGiasApiClient _giasApiClient;
        private static IEstablishmentRepository _establishmentRepository;
        private static ILocalAuthorityRepository _localAuthorityRepository;
        private static IGroupRepository _groupRepository;

        static async Task Run(CommandLineOptions options, CancellationToken cancellationToken = default)
        {
            Init(options);

            var establishments = await GetEstablishments(cancellationToken);
            await StoreEstablishments(establishments, cancellationToken);
            
            await StoreLocalAuthorities(establishments, cancellationToken);

            var groups = await GetGroups(cancellationToken);
            await StoreGroups(groups, cancellationToken);
        }

        static void Init(CommandLineOptions options)
        {
            _giasApiClient = new GiasSoapApiClient(new GiasApiConfiguration
            {
                Url = options.GiasSoapEndpoint,
                Username = options.GiasSoapUsername,
                Password = options.GiasSoapPassword,
                ExtractId = options.ExtractId,
                ExtractEstablishmentsFileName = options.EstablishmentsFileName,
                ExtractGroupsFileName = options.GroupsFileName,
                ExtractGroupLinksFileName = options.GroupLinksFileName,
            });

            var cacheConfiguration = new CacheConfiguration
            {
                TableStorageConnectionString = options.StorageConnectionString,
                EstablishmentTableName = options.EstablishmentsTableName,
                GroupTableName = options.GroupsTableName,
                LocalAuthorityTableName = options.LocalAuthoritiesTableName,
            };
            _establishmentRepository = new TableEstablishmentRepository(cacheConfiguration, _logger);
            _groupRepository = new TableGroupRepository(cacheConfiguration, _logger);
            _localAuthorityRepository = new TableLocalAuthorityRepository(cacheConfiguration, _logger);
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
                var pointInTimeEstablishment = Clone<PointInTimeEstablishment>(establishments[i]);
                pointInTimeEstablishment.PointInTime = DateTime.UtcNow.Date;

                await _establishmentRepository.StoreAsync(pointInTimeEstablishment, cancellationToken);
            }
        }

        static async Task StoreLocalAuthorities(Establishment[] establishments, CancellationToken cancellationToken)
        {
            // Get unique list of local authorities from establishments
            var localAuthorities = establishments
                .Where(e => e.LA != null)
                .Select(e => new LocalAuthority {Code = int.Parse(e.LA.Code), Name = e.LA.DisplayName})
                .GroupBy(la => la.Code)
                .Select(grp => grp.First())
                .ToArray();
            _logger.Debug($"Found {localAuthorities.Length} local authorities in GIAS establishment data");
            
            for (var i = 0; i < localAuthorities.Length; i++)
            {
                _logger.Info($"Storing local authority {i} of {localAuthorities.Length}: {localAuthorities[i].Code}");

                await _localAuthorityRepository.StoreAsync(localAuthorities[i], cancellationToken);
            }
        }
        
        static async Task<Group[]> GetGroups(CancellationToken cancellationToken)
        {
            _logger.Info("Downloading groups...");
            var groups = await _giasApiClient.DownloadGroupsAsync(cancellationToken);
            _logger.Info($"Downloaded {groups.Length} groups");
            return groups;
        }

        static async Task StoreGroups(Group[] groups, CancellationToken cancellationToken)
        {
            for (var i = 0; i < groups.Length; i++)
            {
                _logger.Info($"Storing group {i} of {groups.Length}: {groups[i].Uid}");
                var pointInTimeGroup = Clone<PointInTimeGroup>(groups[i]);
                pointInTimeGroup.PointInTime = DateTime.UtcNow.Date;

                await _groupRepository.StoreAsync(pointInTimeGroup, cancellationToken);
            }
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

                _logger.Info("Done");
            }
        }
    }
}