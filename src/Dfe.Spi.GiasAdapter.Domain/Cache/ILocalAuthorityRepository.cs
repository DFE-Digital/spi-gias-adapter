using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface ILocalAuthorityRepository
    {
        Task StoreAsync(LocalAuthority localAuthority, CancellationToken cancellationToken);
        Task StoreInStagingAsync(PointInTimeLocalAuthority[] localAuthorities, CancellationToken cancellationToken);
        Task<LocalAuthority> GetLocalAuthorityAsync(int laCode, CancellationToken cancellationToken);
        Task<LocalAuthority> GetLocalAuthorityFromStagingAsync(int laCode, CancellationToken cancellationToken);
    }
}