using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.GiasAdapter.Domain.Translation;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Models.Extensions;

namespace Dfe.Spi.GiasAdapter.Application.ManagementGroups
{
    public interface IManagementGroupManager
    {
        Task<ManagementGroup> GetManagementGroupAsync(string id, string fields, CancellationToken cancellationToken);
    }
    
    public class ManagementGroupManager : IManagementGroupManager
    {
        private readonly ITranslator _translator;
        private readonly ILocalAuthorityRepository _localAuthorityRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IMapper _mapper;
        private readonly ILoggerWrapper _logger;

        public ManagementGroupManager(
            ITranslator translator, 
            ILocalAuthorityRepository localAuthorityRepository,
            IGroupRepository groupRepository,
            IMapper mapper, 
            ILoggerWrapper logger)
        {
            _translator = translator;
            _localAuthorityRepository = localAuthorityRepository;
            _groupRepository = groupRepository;
            _mapper = mapper;
            _logger = logger;
        }
        
        public async Task<ManagementGroup> GetManagementGroupAsync(string id, string fields, CancellationToken cancellationToken)
        {
            var typeAndId = SplitCode(id);
            var isLocalAuthority = await IsLocalAuthorityAsync(typeAndId.Key, cancellationToken);
            var managementGroup = isLocalAuthority
                ? await GetLocalAuthorityAsManagementGroupAsync(typeAndId.Value, cancellationToken)
                : await GetGroupAsManagementGroupAsync(typeAndId.Value, cancellationToken);

            if (managementGroup == null)
            {
                return null;
            }
            
            if (!string.IsNullOrEmpty(fields))
            {
                managementGroup = managementGroup.Pick(fields);

                _logger.Info(
                    $"Pruned management group: {managementGroup}.");
            }
            else
            {
                _logger.Debug("No fields specified - model not pruned.");
            }

            return managementGroup;
        }

        private KeyValuePair<string, string> SplitCode(string code)
        {
            var separatorIndex = code.IndexOf('-');
            if (separatorIndex == -1 || separatorIndex == code.Length - 1)
            {
                throw new ArgumentException($"{code} is not a valid management group code", nameof(code));
            }

            var type = code.Substring(0, separatorIndex);
            var id = code.Substring(separatorIndex + 1);
            return new KeyValuePair<string, string>(type, id);
        }

        private async Task<bool> IsLocalAuthorityAsync(string type, CancellationToken cancellationToken)
        {
            var localAuthorityType = await _translator.TranslateEnumValue(
                EnumerationNames.ManagementGroupType, LocalAuthority.ManagementGroupType, cancellationToken);
            return type.Equals(localAuthorityType, StringComparison.InvariantCultureIgnoreCase);
        }

        private async Task<ManagementGroup> GetLocalAuthorityAsManagementGroupAsync(string id,
            CancellationToken cancellationToken)
        {
            int laCode;
            if (!int.TryParse(id, out laCode))
            {
                throw new ArgumentOutOfRangeException($"{id} is not a valid local authority code",
                    nameof(id));
            }

            var localAuthority = await _localAuthorityRepository.GetLocalAuthorityAsync(laCode, cancellationToken);
            if (localAuthority == null)
            {
                return null;
            }

            var managementGroup = await _mapper.MapAsync<ManagementGroup>(localAuthority, cancellationToken);
            return managementGroup;
        }

        private async Task<ManagementGroup> GetGroupAsManagementGroupAsync(string id,
            CancellationToken cancellationToken)
        {
            long uid;
            if (!long.TryParse(id, out uid))
            {
                throw new ArgumentOutOfRangeException($"{id} is not a valid UID",
                    nameof(id));
            }

            var group = await _groupRepository.GetGroupAsync(uid, cancellationToken);
            if (group == null)
            {
                return null;
            }

            var managementGroup = await _mapper.MapAsync<ManagementGroup>(group, cancellationToken);
            return managementGroup;
        }
    }
}