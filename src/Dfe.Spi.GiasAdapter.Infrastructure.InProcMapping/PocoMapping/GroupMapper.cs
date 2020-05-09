using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Translation;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Models.Extensions;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping
{
    public class GroupMapper : ObjectMapper
    {
        private readonly ITranslator _translator;

        public GroupMapper(ITranslator translator)
        {
            _translator = translator;
        }
        
        internal override async Task<TDestination> MapAsync<TDestination>(object source, CancellationToken cancellationToken)
        {
            var group = source as Group;
            if (group == null)
            {
                throw new ArgumentException(
                    $"source must be an Group, but received {source.GetType().FullName}", nameof(source));
            }

            if (typeof(TDestination) != typeof(ManagementGroup))
            {
                throw new ArgumentException(
                    $"TDestination must be ManagementGroup, but received {typeof(TDestination).FullName}",
                    nameof(source));
            }
            
            var managementGroup = new ManagementGroup
            {
                Name = group.GroupName,
                Identifier = group.Uid.ToString(),
                CompaniesHouseNumber = group.CompaniesHouseNumber,
                Ukprn = group.Ukprn,
                AddressLine1 = group.GroupStreet,
                AddressLine2 = group.GroupLocality,
                AddressLine3 = group.GroupAddress3,
                Town = group.GroupTown,
                County = group.GroupCounty,
                Postcode = group.GroupPostcode,
            };
            
            managementGroup.Type =
                await TranslateAsync(EnumerationNames.ManagementGroupType, group.GroupType, cancellationToken);

            managementGroup.Code = $"{managementGroup.Type}-{managementGroup.Identifier}";

            managementGroup.SetLineageForRequestedFields();
            return managementGroup as TDestination;
        }
        
        private async Task<string> TranslateAsync(string enumName, string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return await _translator.TranslateEnumValue(enumName, value, cancellationToken);
        }
    }
}