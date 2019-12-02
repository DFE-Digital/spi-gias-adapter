using AutoMapper;
using Dfe.Spi.GiasAdapter.Domain;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.AutoMapperMapping
{
    internal class EstablishmentMapperProfile : Profile
    {
        public EstablishmentMapperProfile()
        {
            CreateMap<Establishment, LearningProvider>();
        }
    }
}