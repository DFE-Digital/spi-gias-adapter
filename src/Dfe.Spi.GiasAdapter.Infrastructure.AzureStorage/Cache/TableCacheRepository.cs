using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public abstract class TableCacheRepository<TModel, TEntity>
        where TEntity : TableEntity
    {
        protected TableCacheRepository(string connectionString, string tableName, ILoggerWrapper logger)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            Table = tableClient.GetTableReference(tableName);

            Logger = logger;
        }

        protected CloudTable Table { get; private set; }
        protected ILoggerWrapper Logger { get; private set; }


        protected abstract TEntity ModelToEntity(TModel model);
        protected abstract TEntity ModelToEntityForStaging(TModel model);

        protected async Task InsertOrUpdateAsync(TModel model, CancellationToken cancellationToken)
        {
            await Table.CreateIfNotExistsAsync(cancellationToken);
            
            var operation = TableOperation.InsertOrReplace(ModelToEntity(model));
            await Table.ExecuteAsync(operation, cancellationToken);
        }
        protected async Task InsertOrUpdateStagingAsync(TModel[] models, CancellationToken cancellationToken)
        {
            const int batchSize = 100;
            
            await Table.CreateIfNotExistsAsync(cancellationToken);
            
            var partitionedEntities = models
                .Select(ModelToEntityForStaging)
                .GroupBy(entity => entity.PartitionKey)
                .ToDictionary(g => g.Key, g => g.ToArray());
            foreach (var partition in partitionedEntities.Values)
            {
                var position = 0;
                while (position < partition.Length)
                {
                    var entities = partition.Skip(position).Take(batchSize).ToArray();
                    var batch = new TableBatchOperation();

                    foreach (var entity in entities)
                    {
                        batch.InsertOrReplace(entity);
                    }

                    Logger.Debug(
                        $"Inserting {position} to {partition.Length} for partition {entities.First().PartitionKey}");
                    await Table.ExecuteBatchAsync(batch, cancellationToken);

                    position += batchSize;
                }
            }
        }
    }
}