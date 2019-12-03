using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.AutoMapperMapping
{
    public class AutoMapperMapper : Dfe.Spi.GiasAdapter.Domain.Mapping.IMapper
    {
        private static readonly Mapper _mapper;

        static AutoMapperMapper()
        {
            _mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EstablishmentMapperProfile>();
            }));
        }

        public Task<TDestination> MapAsync<TDestination>(object source, CancellationToken cancellationToken)
        {
            try
            {
                var mapped = _mapper.Map<TDestination>(source);
                return Task.FromResult(mapped);
            }
            catch (Exception ex)
            {
                return Task.FromException<TDestination>(ex);
            }
        }
    }
}