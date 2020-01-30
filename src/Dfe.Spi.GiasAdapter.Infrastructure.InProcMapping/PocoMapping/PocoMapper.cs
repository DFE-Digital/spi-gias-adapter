using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.GiasAdapter.Domain.Translation;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping
{
    public class PocoMapper : IMapper
    {
        private readonly ITranslator _translator;

        public PocoMapper(ITranslator translator)
        {
            _translator = translator;
        }

        public async Task<TDestination> MapAsync<TDestination>(object source, CancellationToken cancellationToken)
            where TDestination : class, new()
        {
            var sourceType = source.GetType();
            var mapper = GetMapperForType(sourceType);
            if (mapper == null)
            {
                throw new ArgumentException($"No mapper defined for {sourceType.FullName}", nameof(source));
            }

            return await mapper.MapAsync<TDestination>(source, cancellationToken);
        }

        private ObjectMapper GetMapperForType(Type type)
        {
            if (type == typeof(Establishment))
            {
                return new EstablishmentMapper(_translator);
            }

            return null;
        }
    }
}