using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Translation;
using Dfe.Spi.Models;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping
{
    internal class EstablishmentMapper : ObjectMapper
    {
        private readonly ITranslator _translator;

        public EstablishmentMapper(ITranslator translator)
        {
            _translator = translator;
        }

        internal override async Task<TDestination> MapAsync<TDestination>(object source,
            CancellationToken cancellationToken)
        {
            var establishment = source as Establishment;
            if (establishment == null)
            {
                throw new ArgumentException(
                    $"source must be an Establishment, but received {source.GetType().FullName}", nameof(source));
            }

            if (typeof(TDestination) != typeof(LearningProvider))
            {
                throw new ArgumentException(
                    $"TDestination must be LearningProvider, but received {typeof(TDestination).FullName}",
                    nameof(source));
            }

            var learningProvider = new LearningProvider
            {
                Name = establishment.Name,
                Urn = establishment.Urn,
                Ukprn = establishment.Ukprn,
                Postcode = establishment.Postcode,
                OpenDate = establishment.OpenDate,
                CloseDate = establishment.CloseDate,
            };

            learningProvider.Status = await TranslateCodeNamePairAsync(EnumerationNames.ProviderStatus,
                establishment.EstablishmentStatus, cancellationToken);
            learningProvider.Type = await TranslateCodeNamePairAsync(EnumerationNames.ProviderType,
                establishment.EstablishmentTypeGroup, cancellationToken);
            learningProvider.SubType = await TranslateCodeNamePairAsync(EnumerationNames.ProviderSubType,
                establishment.TypeOfEstablishment, cancellationToken);
            
            return learningProvider as TDestination;
        }

        private async Task<string> TranslateCodeNamePairAsync(string enumName, CodeNamePair codeNamePair,
            CancellationToken cancellationToken)
        {
            if (codeNamePair == null)
            {
                return null;
            }

            return await _translator.TranslateEnumValue(enumName, codeNamePair.Code.ToString(), cancellationToken);
        }
    }
}