using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Events;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.Models.Entities;

namespace Dfe.Spi.GiasAdapter.Application.Cache
{
    public interface ICacheManager
    {
        Task DownloadAllGiasDataToCacheAsync(CancellationToken cancellationToken);
        Task ProcessBatchOfEstablishments(long[] urns, CancellationToken cancellationToken);
        Task ProcessBatchOfGroups(long[] uids, CancellationToken cancellationToken);
        Task ProcessBatchOfLocalAuthorities(int[] laCodes, CancellationToken cancellationToken);
    }

    public class CacheManager : ICacheManager
    {
        private readonly IGiasApiClient _giasApiClient;
        private readonly IEstablishmentRepository _establishmentRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly ILocalAuthorityRepository _localAuthorityRepository;
        private readonly IMapper _mapper;
        private readonly IEventPublisher _eventPublisher;
        private readonly IEstablishmentProcessingQueue _establishmentProcessingQueue;
        private readonly IGroupProcessingQueue _groupProcessingQueue;
        private readonly ILocalAuthorityProcessingQueue _localAuthorityProcessingQueue;
        private readonly ILoggerWrapper _logger;

        public CacheManager(
            IGiasApiClient giasApiClient,
            IEstablishmentRepository establishmentRepository,
            IGroupRepository groupRepository,
            ILocalAuthorityRepository localAuthorityRepository,
            IMapper mapper,
            IEventPublisher eventPublisher,
            IEstablishmentProcessingQueue establishmentProcessingQueue,
            IGroupProcessingQueue groupProcessingQueue,
            ILocalAuthorityProcessingQueue localAuthorityProcessingQueue,
            ILoggerWrapper logger)
        {
            _giasApiClient = giasApiClient;
            _establishmentRepository = establishmentRepository;
            _groupRepository = groupRepository;
            _localAuthorityRepository = localAuthorityRepository;
            _mapper = mapper;
            _eventPublisher = eventPublisher;
            _establishmentProcessingQueue = establishmentProcessingQueue;
            _groupProcessingQueue = groupProcessingQueue;
            _localAuthorityProcessingQueue = localAuthorityProcessingQueue;
            _logger = logger;
        }


        public async Task DownloadAllGiasDataToCacheAsync(CancellationToken cancellationToken)
        {
            var pointInTime = DateTime.UtcNow.Date;

            var groupLinks = await _giasApiClient.DownloadGroupLinksAsync(cancellationToken);

            await DownloadGroupsToCacheAsync(pointInTime, cancellationToken);
            await DownloadEstablishmentsToCacheAsync(pointInTime, groupLinks, cancellationToken);
        }

        public async Task ProcessBatchOfEstablishments(long[] urns, CancellationToken cancellationToken)
        {
            var pointInTime = DateTime.UtcNow.Date; // TODO: FIX!!!
            foreach (var urn in urns)
            {
                var current = await _establishmentRepository.GetEstablishmentAsync(urn, cancellationToken);
                var staging = await _establishmentRepository.GetEstablishmentFromStagingAsync(urn, pointInTime, cancellationToken);

                if (current == null)
                {
                    _logger.Info($"Establishment {urn} has not been seen before. Processing as created");

                    await ProcessEstablishment(staging, _eventPublisher.PublishLearningProviderCreatedAsync,
                        cancellationToken);
                }
                else if (!AreSame(current, staging))
                {
                    _logger.Info($"Establishment {urn} has changed. Processing as updated");

                    await ProcessEstablishment(staging, _eventPublisher.PublishLearningProviderUpdatedAsync,
                        cancellationToken);
                }
                else
                {
                    _logger.Info($"Establishment {urn} has not changed. Skipping");
                }
            }
        }

        public async Task ProcessBatchOfGroups(long[] uids, CancellationToken cancellationToken)
        {
            var pointInTime = DateTime.UtcNow.Date; // TODO: FIX!!!
            foreach (var uid in uids)
            {
                var current = await _groupRepository.GetGroupAsync(uid, cancellationToken);
                var staging = await _groupRepository.GetGroupFromStagingAsync(uid, pointInTime, cancellationToken);

                if (current == null)
                {
                    _logger.Info($"Group {uid} has not been seen before. Processing as created");

                    await ProcessGroup(staging, _eventPublisher.PublishManagementGroupCreatedAsync,
                        cancellationToken);
                }
                else if (!AreSame(current, staging))
                {
                    _logger.Info($"Group {uid} has changed. Processing as updated");

                    await ProcessGroup(staging, _eventPublisher.PublishManagementGroupUpdatedAsync,
                        cancellationToken);
                }
                else
                {
                    _logger.Info($"Group {uid} has not changed. Skipping");
                }
            }
        }

        public async Task ProcessBatchOfLocalAuthorities(int[] laCodes, CancellationToken cancellationToken)
        {
            foreach (var laCode in laCodes)
            {
                var current = await _localAuthorityRepository.GetLocalAuthorityAsync(laCode, cancellationToken);
                var staging = await _localAuthorityRepository.GetLocalAuthorityFromStagingAsync(laCode, cancellationToken);

                if (current == null)
                {
                    _logger.Info($"Local authority {laCode} has not been seen before. Processing as created");

                    await ProcessLocalAuthority(staging, _eventPublisher.PublishManagementGroupCreatedAsync,
                        cancellationToken);
                }
                else if (!AreSame(current, staging))
                {
                    _logger.Info($"Local authority {laCode} has changed. Processing as updated");

                    await ProcessLocalAuthority(staging, _eventPublisher.PublishManagementGroupUpdatedAsync,
                        cancellationToken);
                }
                else
                {
                    _logger.Info($"Local authority {laCode} has not changed. Skipping");
                }
            }
        }


        private async Task DownloadGroupsToCacheAsync(DateTime pointInTime, CancellationToken cancellationToken)
        {
            _logger.Info("Acquiring groups file from GIAS...");

            // Download
            var groups = await _giasApiClient.DownloadGroupsAsync(cancellationToken);
            _logger.Debug($"Downloaded {groups.Length} groups from GIAS");

            // Timestamp
            var pointInTimeGroups = groups.Select(group => group.Clone<PointInTimeGroup>()).ToArray();
            foreach (var pointInTimeGroup in pointInTimeGroups)
            {
                pointInTimeGroup.PointInTime = pointInTime;
            }

            // Store
            await _groupRepository.StoreInStagingAsync(pointInTimeGroups, cancellationToken);
            _logger.Debug($"Stored {pointInTimeGroups.Length} groups in staging");

            // Queue diff check
            var position = 0;
            const int batchSize = 100;
            while (position < groups.Length)
            {
                var batch = groups
                    .Skip(position)
                    .Take(batchSize)
                    .Select(e => e.Uid)
                    .ToArray();

                _logger.Debug(
                    $"Queuing {position} to {position + batch.Length} of groups for processing");
                await _groupProcessingQueue.EnqueueBatchOfStagingAsync(batch, cancellationToken);

                position += batchSize;
            }

            _logger.Info("Finished downloading groups to cache");
        }

        private async Task DownloadEstablishmentsToCacheAsync(DateTime pointInTime, GroupLink[] groupLinks,
            CancellationToken cancellationToken)
        {
            _logger.Info("Acquiring establishments file from GIAS...");

            // Download
            var establishments = await _giasApiClient.DownloadEstablishmentsAsync(cancellationToken);
            _logger.Debug($"Downloaded {establishments.Length} establishments from GIAS");

            // Timestamp
            var pointInTimeEstablishments = establishments.Select(establishment => establishment.Clone<PointInTimeEstablishment>()).ToArray();
            foreach (var pointInTimeEstablishment in pointInTimeEstablishments)
            {
                pointInTimeEstablishment.PointInTime = pointInTime;
            }

            // Add links
            foreach (var establishment in pointInTimeEstablishments)
            {
                var establishmentGroupLinks = groupLinks.Where(l => l.Urn == establishment.Urn).ToArray();
                var federationLink = establishmentGroupLinks.FirstOrDefault(l => l.GroupType == "Federation");
                var trustLink = establishmentGroupLinks.FirstOrDefault(l =>
                    l.GroupType == "Trust" || l.GroupType == "Single-academy trust" || l.GroupType == "Multi-academy trust");

                if (federationLink != null)
                {
                    establishment.Federations = new CodeNamePair
                    {
                        Code = federationLink.Uid.ToString(),
                    };
                    _logger.Debug($"Set Federations to {federationLink.Uid} from links of establishment {establishment.Urn}");
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

            // Process local authorities
            await ProcessEstablishmentLocalAuthoritiesToCacheAsync(pointInTime, pointInTimeEstablishments, cancellationToken);

            // Store
            await _establishmentRepository.StoreInStagingAsync(pointInTimeEstablishments, cancellationToken);
            _logger.Debug($"Stored {pointInTimeEstablishments.Length} establishments in staging");

            // Queue diff check
            var position = 0;
            const int batchSize = 100;
            while (position < establishments.Length)
            {
                var batch = establishments
                    .Skip(position)
                    .Take(batchSize)
                    .Select(e => e.Urn)
                    .ToArray();

                _logger.Debug(
                    $"Queuing {position} to {position + batch.Length} of establishments for processing");
                await _establishmentProcessingQueue.EnqueueBatchOfStagingAsync(batch, cancellationToken);

                position += batchSize;
            }

            _logger.Info("Finished downloading Establishments to cache");
        }

        private async Task ProcessEstablishmentLocalAuthoritiesToCacheAsync(DateTime pointInTime, Establishment[] establishments,
            CancellationToken cancellationToken)
        {
            // Get unique list of local authorities from establishments
            var localAuthorities = establishments
                .Where(e => e.LA != null)
                .Select(e => new LocalAuthority {Code = int.Parse(e.LA.Code), Name = e.LA.DisplayName})
                .GroupBy(la => la.Code)
                .Select(grp => grp.First())
                .ToArray();
            _logger.Debug($"Found {localAuthorities.Length} local authorities in GIAS establishment data");

            // Timestamp
            var pointInTimeLocalAuthorities = localAuthorities.Select(localAuthority => localAuthority.Clone<PointInTimeLocalAuthority>()).ToArray();
            foreach (var pointInTimeLocalAuthority in pointInTimeLocalAuthorities)
            {
                pointInTimeLocalAuthority.PointInTime = pointInTime;
            }

            // Store
            await _localAuthorityRepository.StoreInStagingAsync(pointInTimeLocalAuthorities, cancellationToken);
            _logger.Debug($"Stored {pointInTimeLocalAuthorities.Length} local authorities in staging");

            // Queue diff check
            var position = 0;
            const int batchSize = 100;
            while (position < localAuthorities.Length)
            {
                var batch = localAuthorities
                    .Skip(position)
                    .Take(batchSize)
                    .Select(e => e.Code)
                    .ToArray();

                _logger.Debug(
                    $"Queuing {position} to {position + batch.Length} of local authorities for processing");
                await _localAuthorityProcessingQueue.EnqueueBatchOfStagingAsync(batch, cancellationToken);

                position += batchSize;
            }

            _logger.Info("Finished processing local authorities to cache");
        }

        private bool AreSame(Establishment current, Establishment staging)
        {
            if (current.EstablishmentName != staging.EstablishmentName)
            {
                return false;
            }

            if (current.Ukprn != staging.Ukprn)
            {
                return false;
            }

            if (current.Uprn != staging.Uprn)
            {
                return false;
            }

            if (current.CompaniesHouseNumber != staging.CompaniesHouseNumber)
            {
                return false;
            }

            if (current.CharitiesCommissionNumber != staging.CharitiesCommissionNumber)
            {
                return false;
            }

            if (current.Trusts?.Code != staging.Trusts?.Code)
            {
                return false;
            }

            if (current.LA?.Code != staging.LA?.Code)
            {
                return false;
            }

            if (current.EstablishmentNumber != staging.EstablishmentNumber)
            {
                return false;
            }

            if (current.PreviousEstablishmentNumber != staging.PreviousEstablishmentNumber)
            {
                return false;
            }

            if (current.Postcode != staging.Postcode)
            {
                return false;
            }

            if (current.Federations?.Code != staging.Federations?.Code)
            {
                return false;
            }

            if (current.Trusts?.Code != staging.Trusts?.Code)
            {
                return false;
            }

            return true;
        }

        private bool AreSame(Group current, Group staging)
        {
            return current.GroupName == staging.GroupName &&
                   current.GroupType == staging.GroupType &&
                   current.CompaniesHouseNumber == staging.CompaniesHouseNumber &&
                   current.Ukprn == staging.Ukprn &&
                   current.GroupStreet == staging.GroupStreet &&
                   current.GroupLocality == staging.GroupLocality &&
                   current.GroupAddress3 == staging.GroupAddress3 &&
                   current.GroupTown == staging.GroupTown &&
                   current.GroupCounty == staging.GroupCounty &&
                   current.GroupPostcode == staging.GroupPostcode;
        }

        private bool AreSame(LocalAuthority current, LocalAuthority staging)
        {
            return current.Name == staging.Name;
        }

        private async Task ProcessEstablishment(PointInTimeEstablishment staging,
            Func<LearningProvider, CancellationToken, Task> publishEvent,
            CancellationToken cancellationToken)
        {
            await _establishmentRepository.StoreAsync(staging, cancellationToken);
            _logger.Debug($"Stored establishment {staging.Urn} in repository");

            var learningProvider = await _mapper.MapAsync<LearningProvider>(staging, cancellationToken);
            learningProvider._Lineage = null;
            await publishEvent(learningProvider, cancellationToken);
            _logger.Debug($"Sent event for establishment {staging.Urn}");
        }

        private async Task ProcessGroup(PointInTimeGroup staging,
            Func<ManagementGroup, CancellationToken, Task> publishEvent,
            CancellationToken cancellationToken)
        {
            await _groupRepository.StoreAsync(staging, cancellationToken);
            _logger.Debug($"Stored group {staging.Uid} in repository");

            var managementGroup = await _mapper.MapAsync<ManagementGroup>(staging, cancellationToken);
            managementGroup._Lineage = null;
            await publishEvent(managementGroup, cancellationToken);
            _logger.Debug($"Sent event for group {staging.Uid}");
        }

        private async Task ProcessLocalAuthority(LocalAuthority staging,
            Func<ManagementGroup, CancellationToken, Task> publishEvent,
            CancellationToken cancellationToken)
        {
            await _localAuthorityRepository.StoreAsync(staging, cancellationToken);
            _logger.Debug($"Stored local authority {staging.Code} in repository");

            var managementGroup = await _mapper.MapAsync<ManagementGroup>(staging, cancellationToken);
            managementGroup._Lineage = null;
            await publishEvent(managementGroup, cancellationToken);
            _logger.Debug($"Sent event for local authority {staging.Code}");
        }
    }
}