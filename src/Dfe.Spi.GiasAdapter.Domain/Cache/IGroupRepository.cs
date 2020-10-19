using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IGroupRepository
    {
        Task StoreAsync(PointInTimeGroup group, CancellationToken cancellationToken);
        Task StoreAsync(PointInTimeGroup[] groups, CancellationToken cancellationToken);
        Task StoreInStagingAsync(PointInTimeGroup[] groups, CancellationToken cancellationToken);
        Task<PointInTimeGroup> GetGroupAsync(long uid, CancellationToken cancellationToken);
        Task<PointInTimeGroup> GetGroupAsync(long uid, DateTime? pointInTime, CancellationToken cancellationToken);
        Task<PointInTimeGroup> GetGroupFromStagingAsync(long uid, DateTime pointInTime, CancellationToken cancellationToken);
        
        Task<int> ClearStagingDataForDateAsync(DateTime date, CancellationToken cancellationToken);
    }
}