using System;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.Models;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping
{
    internal class EstablishmentMapper : ObjectMapper
    {
        protected override TDestination Map<TDestination>(object source)
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
                    $"TDestination must be LearningProvider, but received {typeof(TDestination).FullName}", nameof(source));
            }

            var learningProvider = new LearningProvider
            {
                Name = establishment.Name,
            };
            return learningProvider as TDestination;
        }
    }
}