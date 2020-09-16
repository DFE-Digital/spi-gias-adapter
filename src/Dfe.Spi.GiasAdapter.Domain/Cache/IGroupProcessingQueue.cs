using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IGroupProcessingQueue
    {
        Task EnqueueStagingAsync(StagingBatchQueueItem<long> queueItem, CancellationToken cancellationToken);
    }
}