using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IStateRepository
    {
        Task<DateTime> GetLastStagingDateClearedAsync(string entityType, CancellationToken cancellationToken);
        Task SetLastStagingDateClearedAsync(string entityType, DateTime lastRead, CancellationToken cancellationToken);
    }
}