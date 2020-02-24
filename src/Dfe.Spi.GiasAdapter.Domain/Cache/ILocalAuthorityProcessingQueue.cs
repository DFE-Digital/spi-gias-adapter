using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface ILocalAuthorityProcessingQueue
    {
        Task EnqueueBatchOfStagingAsync(int[] laCodes, CancellationToken cancellationToken);
    }
}