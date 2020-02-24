using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public class QueueEstablishmentProcessingQueue : IEstablishmentProcessingQueue
    {
        private CloudQueue _queue;
        
        public QueueEstablishmentProcessingQueue(CacheConfiguration configuration)
        {
            var storageAccount = CloudStorageAccount.Parse(configuration.ProcessingQueueConnectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference(CacheQueueNames.EstablishmentProcessingQueue);
        }
        
        public async Task EnqueueBatchOfStagingAsync(long[] urns, CancellationToken cancellationToken)
        {
            await _queue.CreateIfNotExistsAsync(cancellationToken);
                
            var message = new CloudQueueMessage(JsonConvert.SerializeObject(urns));
            await _queue.AddMessageAsync(message, cancellationToken);
        }
    }
}