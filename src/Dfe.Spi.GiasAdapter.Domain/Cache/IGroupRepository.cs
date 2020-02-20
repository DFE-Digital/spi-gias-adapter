using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IGroupRepository
    {
        Task StoreInStagingAsync(Group[] groups, CancellationToken cancellationToken);
    }
}