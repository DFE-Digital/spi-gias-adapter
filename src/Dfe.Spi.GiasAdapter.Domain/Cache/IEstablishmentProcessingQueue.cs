using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IEstablishmentProcessingQueue
    {
        Task EnqueueBatchOfStagingAsync(StagingBatchQueueItem<long> queueItem, CancellationToken cancellationToken);
    }
}