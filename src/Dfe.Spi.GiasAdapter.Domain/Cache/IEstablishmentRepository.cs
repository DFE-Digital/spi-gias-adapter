using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IEstablishmentRepository
    {
        Task StoreInStagingAsync(Establishment[] establishments, CancellationToken cancellationToken);
    }
}