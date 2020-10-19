using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface ILocalAuthorityRepository
    {
        Task StoreAsync(PointInTimeLocalAuthority localAuthority, CancellationToken cancellationToken);
        Task StoreAsync(PointInTimeLocalAuthority[] localAuthorities, CancellationToken cancellationToken);
        Task StoreInStagingAsync(PointInTimeLocalAuthority[] localAuthorities, CancellationToken cancellationToken);
        Task<PointInTimeLocalAuthority> GetLocalAuthorityAsync(int laCode, CancellationToken cancellationToken);
        Task<PointInTimeLocalAuthority> GetLocalAuthorityAsync(int laCode, DateTime? pointInTime, CancellationToken cancellationToken);
        Task<PointInTimeLocalAuthority> GetLocalAuthorityFromStagingAsync(int laCode, DateTime pointInTime, CancellationToken cancellationToken);
        
        Task<int> ClearStagingDataForDateAsync(DateTime date, CancellationToken cancellationToken);
    }
}