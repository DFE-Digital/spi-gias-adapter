using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IGroupProcessingQueue
    {
        Task EnqueueBatchOfStagingAsync(long[] uids, CancellationToken cancellationToken);
    }
}