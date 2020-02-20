using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IGroupRepository
    {
        Task StoreAsync(Group group, CancellationToken cancellationToken);
        Task StoreInStagingAsync(Group[] groups, CancellationToken cancellationToken);
        Task<Group> GetGroupAsync(long uid, CancellationToken cancellationToken);
        Task<Group> GetGroupFromStagingAsync(long uid, CancellationToken cancellationToken);
    }
}