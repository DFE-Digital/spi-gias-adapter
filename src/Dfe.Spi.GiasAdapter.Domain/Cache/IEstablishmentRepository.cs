using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IEstablishmentRepository
    {
        Task StoreAsync(Establishment establishment, CancellationToken cancellationToken);
        Task StoreAsync(Establishment[] establishments, CancellationToken cancellationToken);
        Task StoreInStagingAsync(Establishment[] establishments, CancellationToken cancellationToken);
        Task<Establishment> GetEstablishmentAsync(long urn, CancellationToken cancellationToken);
        Task<Establishment> GetEstablishmentFromStagingAsync(long urn, CancellationToken cancellationToken);
    }
}