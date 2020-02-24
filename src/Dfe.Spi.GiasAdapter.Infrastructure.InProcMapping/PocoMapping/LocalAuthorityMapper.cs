using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Translation;
using Dfe.Spi.Models.Entities;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping
{
    public class LocalAuthorityMapper : ObjectMapper
    {
        private readonly ITranslator _translator;

        public LocalAuthorityMapper(ITranslator translator)
        {
            _translator = translator;
        }
        
        internal override async Task<TDestination> MapAsync<TDestination>(object source, CancellationToken cancellationToken)
        {
            var group = source as LocalAuthority;
            if (group == null)
            {
                throw new ArgumentException(
                    $"source must be an LocalAuthority, but received {source.GetType().FullName}", nameof(source));
            }

            if (typeof(TDestination) != typeof(ManagementGroup))
            {
                throw new ArgumentException(
                    $"TDestination must be ManagementGroup, but received {typeof(TDestination).FullName}",
                    nameof(source));
            }
            
            var managementGroup = new ManagementGroup
            {
                Name = group.Name,
                Identifier = group.Code.ToString(),
            };
            
            managementGroup.Type =
                await TranslateAsync(EnumerationNames.ManagementGroupType, LocalAuthority.ManagementGroupType, cancellationToken);

            managementGroup.Code = $"{managementGroup.Type}-{managementGroup.Identifier}";

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