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
        Task ProcessGroupAsync(long uid, long[] urns, DateTime pointInTime, CancellationToken cancellationToken);
        Task ProcessLocalAuthorityAsync(int laCode, long[] urns, DateTime pointInTime, CancellationToken cancellationToken);
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

            var groups = await DownloadGroupsToCacheAsync(pointInTime, cancellationToken);
            var establishments = await DownloadEstablishmentsToCacheAsync(pointInTime, groupLinks, cancellationToken);
            var localAuthorities = await ProcessEstablishmentLocalAuthoritiesToCacheAsync(pointInTime, establishments, cancellationToken);

            await EnqueueGroupsAsync(groups, establishments, cancellationToken);
            await EnqueueLocalAuthoritiesAsync(localAuthorities, establishments, cancellationToken);
        }

        public async Task ProcessGroupAsync(long uid, long[] urns, DateTime pointInTime, CancellationToken cancellationToken)
        {
            // Check if group changed
            var previous = await _groupRepository.GetGroupAsync(uid, pointInTime, cancellationToken);
            var staging = await _groupRepository.GetGroupFromStagingAsync(uid, pointInTime, cancellationToken);
            var groupChanged = false;

            if (previous == null)
            {
                _logger.Info($"Group {uid} has not been seen before {pointInTime}. Processing as created");

                await StoreGroupAndRaiseEventAsync(staging, false, cancellationToken);
                groupChanged = true;
            }
            else if (!AreSame(previous, staging))
            {
                _logger.Info($"Group {uid} on {pointInTime} has changed since {previous.PointInTime}. Processing as updated");

                await StoreGroupAndRaiseEventAsync(staging, true, cancellationToken);
                groupChanged = true;
            }
            else
            {
                _logger.Info($"Group {uid} on {pointInTime} has not changed since {previous.PointInTime}. Skipping");
            }

            // Check establishments for change
            await ProcessEstablishmentsAsync(urns, pointInTime, groupChanged, cancellationToken);
        }

        public async Task ProcessLocalAuthorityAsync(int laCode, long[] urns, DateTime pointInTime, CancellationToken cancellationToken)
        {
            // Check if local authority changed
            var previous = await _localAuthorityRepository.GetLocalAuthorityAsync(laCode, pointInTime, cancellationToken);
            var staging = await _localAuthorityRepository.GetLocalAuthorityFromStagingAsync(laCode, pointInTime, cancellationToken);
            var localAuthorityChanged = false;

            if (previous == null)
            {
                _logger.Info($"Local authority {laCode} has not been seen before {pointInTime}. Processing as created");

                await StoreLocalAuthorityAndRaiseEventAsync(staging, false, cancellationToken);
                localAuthorityChanged = true;
            }
            else if (!AreSame(previous, staging))
            {
                _logger.Info($"Local authority {laCode} on {pointInTime} has changed since {previous.PointInTime}. Processing as updated");

                await StoreLocalAuthorityAndRaiseEventAsync(staging, true, cancellationToken);
                localAuthorityChanged = true;
            }
            else
            {
                _logger.Info($"Local authority {laCode} on {pointInTime} has not changed since {previous.PointInTime}. Skipping");
            }

            // Check establishments for change
            await ProcessEstablishmentsAsync(urns, pointInTime, localAuthorityChanged, cancellationToken);
        }


        private async Task<PointInTimeGroup[]> DownloadGroupsToCacheAsync(DateTime pointInTime, CancellationToken cancellationToken)
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

            _logger.Info("Finished downloading groups to cache");
            return pointInTimeGroups;
        }

        private async Task<PointInTimeEstablishment[]> DownloadEstablishmentsToCacheAsync(DateTime pointInTime, GroupLink[] groupLinks,
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

            // Store
            await _establishmentRepository.StoreInStagingAsync(pointInTimeEstablishments, cancellationToken);
            _logger.Debug($"Stored {pointInTimeEstablishments.Length} establishments in staging");

            _logger.Info("Finished downloading Establishments to cache");
            return pointInTimeEstablishments;
        }

        private async Task<PointInTimeLocalAuthority[]> ProcessEstablishmentLocalAuthoritiesToCacheAsync(DateTime pointInTime, Establishment[] establishments,
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

            _logger.Info("Finished processing local authorities to cache");
            return pointInTimeLocalAuthorities;
        }

        private async Task EnqueueGroupsAsync(PointInTimeGroup[] groups, PointInTimeEstablishment[] establishments, CancellationToken cancellationToken)
        {
            foreach (var group in groups)
            {
                var urns = establishments
                    .Where(e => e.Trusts?.Code == group.Uid.ToString() ||
                                e.Federations?.Code == group.Uid.ToString())
                    .Select(e => e.Urn)
                    .ToArray();

                await _groupProcessingQueue.EnqueueStagingAsync(
                    new StagingBatchQueueItem<long>
                    {
                        ParentIdentifier = group.Uid,
                        Urns = urns,
                        PointInTime = group.PointInTime,
                    },
                    cancellationToken);
                _logger.Debug($"Queued group {group.Uid} at {group.PointInTime} with {urns.Length} child urns");
            }
        }

        private async Task EnqueueLocalAuthoritiesAsync(PointInTimeLocalAuthority[] localAuthorities, PointInTimeEstablishment[] establishments,
            CancellationToken cancellationToken)
        {
            foreach (var localAuthority in localAuthorities)
            {
                var urns = establishments
                    .Where(e => string.IsNullOrEmpty(e.Trusts?.Code) &&
                                string.IsNullOrEmpty(e.Federations?.Code) &&
                                e.LA.Code == localAuthority.Code.ToString())
                    .Select(e => e.Urn)
                    .ToArray();

                await _localAuthorityProcessingQueue.EnqueueStagingAsync(
                    new StagingBatchQueueItem<int>
                    {
                        ParentIdentifier = localAuthority.Code,
                        Urns = urns,
                        PointInTime = localAuthority.PointInTime,
                    },
                    cancellationToken);
                _logger.Debug($"Queued local authority {localAuthority.Code} at {localAuthority.PointInTime} with {urns.Length} child urns");
            }
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

        private async Task ProcessEstablishmentsAsync(
            long[] urns,
            DateTime pointInTime,
            bool managementGroupHasChanged,
            CancellationToken cancellationToken)
        {
            foreach (var urn in urns)
            {
                var previous = await _establishmentRepository.GetEstablishmentAsync(urn, pointInTime, cancellationToken);
                var staging = await _establishmentRepository.GetEstablishmentFromStagingAsync(urn, pointInTime, cancellationToken);

                if (previous == null)
                {
                    _logger.Info($"Establishment {urn} has not been seen before {pointInTime}. Processing as created");

                    await StoreEstablishmentAndRaiseEventAsync(staging, false, cancellationToken);
                }
                else if (managementGroupHasChanged)
                {
                    _logger.Info($"Management group of establishment {urn} on {pointInTime} has changed. Processing as updated");

                    await StoreEstablishmentAndRaiseEventAsync(staging, true, cancellationToken);
                }
                else if (!AreSame(previous, staging))
                {
                    _logger.Info($"Establishment {urn} on {pointInTime} has changed since {previous.PointInTime}. Processing as updated");

                    await StoreEstablishmentAndRaiseEventAsync(staging, true, cancellationToken);
                }
                else
                {
                    _logger.Info($"Establishment {urn} on {pointInTime} has not changed since {previous.PointInTime}. Skipping");
                }
            }
        }

        private async Task StoreEstablishmentAndRaiseEventAsync(
            PointInTimeEstablishment staging,
            bool isUpdate,
            CancellationToken cancellationToken)
        {
            var current = await _establishmentRepository.GetEstablishmentAsync(staging.Urn, cancellationToken);

            staging.IsCurrent = current == null || staging.PointInTime > current.PointInTime;
            if (current != null && staging.IsCurrent)
            {
                current.IsCurrent = false;
            }

            var toStore = current == null || current.IsCurrent
                ? new[] {staging}
                : new[] {current, staging};

            await _establishmentRepository.StoreAsync(toStore, cancellationToken);
            _logger.Debug($"Stored establishment {staging.Urn} in repository");

            var learningProvider = await _mapper.MapAsync<LearningProvider>(staging, cancellationToken);
            learningProvider._Lineage = null;

            if (isUpdate)
            {
                await _eventPublisher.PublishLearningProviderUpdatedAsync(learningProvider, staging.PointInTime, cancellationToken);
            }
            else
            {
                await _eventPublisher.PublishLearningProviderCreatedAsync(learningProvider, staging.PointInTime, cancellationToken);
            }

            _logger.Debug($"Sent event for establishment {staging.Urn}");
        }

        private async Task StoreGroupAndRaiseEventAsync(
            PointInTimeGroup staging,
            bool isUpdate,
            CancellationToken cancellationToken)
        {
            var current = await _groupRepository.GetGroupAsync(staging.Uid, cancellationToken);

            staging.IsCurrent = current == null || staging.PointInTime > current.PointInTime;
            if (current != null && staging.IsCurrent)
            {
                current.IsCurrent = false;
            }

            var toStore = current == null || current.IsCurrent
                ? new[] {staging}
                : new[] {current, staging};

            await _groupRepository.StoreAsync(toStore, cancellationToken);
            _logger.Debug($"Stored group {staging.Uid} in repository");

            var managementGroup = await _mapper.MapAsync<ManagementGroup>(staging, cancellationToken);
            managementGroup._Lineage = null;

            if (isUpdate)
            {
                await _eventPublisher.PublishManagementGroupUpdatedAsync(managementGroup, staging.PointInTime, cancellationToken);
            }
            else
            {
                await _eventPublisher.PublishManagementGroupCreatedAsync(managementGroup, staging.PointInTime, cancellationToken);
            }

            _logger.Debug($"Sent event for group {staging.Uid}");
        }

        private async Task StoreLocalAuthorityAndRaiseEventAsync(
            PointInTimeLocalAuthority staging,
            bool isUpdate,
            CancellationToken cancellationToken)
        {
            var current = await _localAuthorityRepository.GetLocalAuthorityAsync(staging.Code, cancellationToken);

            staging.IsCurrent = current == null || staging.PointInTime > current.PointInTime;
            if (current != null && staging.IsCurrent)
            {
                current.IsCurrent = false;
            }

            var toStore = current == null || current.IsCurrent
                ? new[] {staging}
                : new[] {current, staging};

            await _localAuthorityRepository.StoreAsync(toStore, cancellationToken);
            _logger.Debug($"Stored local authority {staging.Code} in repository");

            var managementGroup = await _mapper.MapAsync<ManagementGroup>(staging, cancellationToken);
            managementGroup._Lineage = null;

            if (isUpdate)
            {
                await _eventPublisher.PublishManagementGroupUpdatedAsync(managementGroup, staging.PointInTime, cancellationToken);
            }
            else
            {
                await _eventPublisher.PublishManagementGroupCreatedAsync(managementGroup, staging.PointInTime, cancellationToken);
            }

            _logger.Debug($"Sent event for local authority {staging.Code}");
        }
    }
}