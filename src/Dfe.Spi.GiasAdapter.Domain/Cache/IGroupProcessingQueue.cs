using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IGroupProcessingQueue
    {
        Task EnqueueBatchOfStagingAsync(StagingBatchQueueItem<long> queueItem, CancellationToken cancellationToken);
    }
}