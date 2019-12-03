using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi
{
    public class GiasSoapApiClient : IGiasApiClient
    {
        public Task<Establishment> GetEstablishmentAsync(int urn, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Establishment
            {
                Urn = urn,
                Name = "Some School",
            });
        }
    }
}