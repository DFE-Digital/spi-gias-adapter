using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.GiasApi
{
    public interface IGiasApiClient
    {
        Task<Establishment> GetEstablishmentAsync(long urn, CancellationToken cancellationToken);
    }
}