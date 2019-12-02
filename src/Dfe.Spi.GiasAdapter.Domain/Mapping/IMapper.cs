using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Mapping
{
    public interface IMapper
    {
        Task<TDestination> MapAsync<TDestination>(object source, CancellationToken cancellationToken);
    }
}