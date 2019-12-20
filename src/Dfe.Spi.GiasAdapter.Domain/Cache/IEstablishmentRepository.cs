using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IEstablishmentRepository
    {
        Task StoreAsync(Establishment establishment, CancellationToken cancellationToken);
        Task StoreInStagingAsync(Establishment[] establishments, CancellationToken cancellationToken);
        Task<Establishment> GetEstablishment(long urn, CancellationToken cancellationToken);
        Task<Establishment> GetEstablishmentFromStaging(long urn, CancellationToken cancellationToken);
    }
}