using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public abstract class TableCacheRepository<TModel, TEntity>
        where TModel : class
        where TEntity : TableEntity, new()
    {
        private readonly string _logTypeName;

        protected TableCacheRepository(string connectionString, string tableName, ILoggerWrapper logger, string logTypeName)
        {
            _logTypeName = logTypeName;
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            Table = tableClient.GetTableReference(tableName);

            Logger = logger;
        }

        protected CloudTable Table { get; private set; }
        protected ILoggerWrapper Logger { get; private set; }


        protected abstract TEntity ModelToEntity(TModel model);
        protected abstract TEntity ModelToEntityForStaging(TModel model);
        protected abstract TModel EntityToModel(TEntity entity);

        protected virtual TEntity[] ProcessEntitiesBeforeStoring(TEntity[] entities)
        {
            return entities;
        }


        protected async Task<int> DeleteAllRowsInPartitionAsync(string partitionKey, CancellationToken cancellationToken)
        {
            var query = new TableQuery<TableEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey))
                .Select(new[] {"PartitionKey", "RowKey"});
            var rows = await QueryTableAsync(query, cancellationToken);
            
            var index = 0;
            while (index < rows.Length)
            {
                var batchOfRows = rows.Skip(index).Take(100).ToArray();
                var batchOperation = new TableBatchOperation();
            
                foreach (var row in batchOfRows)
                {
                    batchOperation.Delete(row);
                }
            
                await Table.ExecuteBatchAsync(batchOperation, cancellationToken);
                index += batchOfRows.Length;
            }
            
            return rows.Length;
        }

        protected async Task InsertOrUpdateAsync(TModel model, CancellationToken cancellationToken)
        {
            await Table.CreateIfNotExistsAsync(cancellationToken);

            var operation = TableOperation.InsertOrReplace(ModelToEntity(model));
            await Table.ExecuteAsync(operation, cancellationToken);
        }

        protected async Task InsertOrUpdateAsync(TModel[] models, CancellationToken cancellationToken)
        {
            const int batchSize = 100;

            await Table.CreateIfNotExistsAsync(cancellationToken);

            var entities = models.Select(ModelToEntity).ToArray();
            var processedEntities = ProcessEntitiesBeforeStoring(entities);

            var partitionedEntities = processedEntities
                .GroupBy(entity => entity.PartitionKey)
                .ToDictionary(g => g.Key, g => g.ToArray());
            foreach (var partition in partitionedEntities.Values)
            {
                var position = 0;
                while (position < partition.Length)
                {
                    var batchOfEntities = partition.Skip(position).Take(batchSize).ToArray();
                    var batch = new TableBatchOperation();

                    foreach (var entity in batchOfEntities)
                    {
                        batch.InsertOrReplace(entity);
                    }

                    Logger.Debug(
                        $"Inserting {position} to {partition.Length} for partition {batchOfEntities.First().PartitionKey} of {_logTypeName}");
                    await Table.ExecuteBatchAsync(batch, cancellationToken);

                    position += batchSize;
                }
            }
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
                        $"Inserting {position} to {partition.Length} for partition {entities.First().PartitionKey} of {_logTypeName}");
                    await Table.ExecuteBatchAsync(batch, cancellationToken);

                    position += batchSize;
                }
            }
        }

        protected async Task<TModel> RetrieveAsync(string partitionKey, string rowKey, CancellationToken cancellationToken)
        {
            var operation = TableOperation.Retrieve<TEntity>(partitionKey, rowKey);
            var operationResult = await Table.ExecuteAsync(operation, cancellationToken);
            var entity = (TEntity) operationResult.Result;

            return entity == null ? null : EntityToModel(entity);
        }

        protected async Task<TModel[]> QueryAsync(TableQuery<TEntity> query, CancellationToken cancellationToken)
        {
            var results = await QueryTableAsync(query, cancellationToken);

            return results
                .Select(EntityToModel)
                .ToArray();
        }

        private async Task<T[]> QueryTableAsync<T>(TableQuery<T> query, CancellationToken cancellationToken) where T : TableEntity, new()
        {
            var nextQuery = query;
            var continuationToken = default(TableContinuationToken);
            var results = new List<T>();

            do
            {
                var result = await Table.ExecuteQuerySegmentedAsync(nextQuery, continuationToken, cancellationToken);

                results.AddRange(result.Results);

                continuationToken = result.ContinuationToken;
            } while (continuationToken != null && !cancellationToken.IsCancellationRequested);

            return results.ToArray();
        }
    }
}