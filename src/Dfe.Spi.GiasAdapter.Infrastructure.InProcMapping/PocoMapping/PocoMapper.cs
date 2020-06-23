using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.GiasAdapter.Domain.Translation;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping
{
    public class PocoMapper : IMapper
    {
        private readonly ITranslator _translator;
        private readonly IGroupRepository _groupRepository;
        private readonly ILocalAuthorityRepository _localAuthorityRepository;

        public PocoMapper(
            ITranslator translator,
            IGroupRepository groupRepository,
            ILocalAuthorityRepository localAuthorityRepository)
        {
            _translator = translator;
            _groupRepository = groupRepository;
            _localAuthorityRepository = localAuthorityRepository;
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
            if (type == typeof(Establishment) || type == typeof(PointInTimeEstablishment))
            {
                return new EstablishmentMapper(_translator, _groupRepository, _localAuthorityRepository, this);
            }
            if (type == typeof(Group) || type == typeof(PointInTimeGroup))
            {
                return new GroupMapper(_translator);
            }
            if (type == typeof(LocalAuthority) || type == typeof(PointInTimeLocalAuthority))
            {
                return new LocalAuthorityMapper(_translator);
            }

            return null;
        }
    }
}