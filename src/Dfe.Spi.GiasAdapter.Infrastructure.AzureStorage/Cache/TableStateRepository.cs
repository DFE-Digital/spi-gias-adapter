using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public class TableStateRepository : IStateRepository
    {
        private readonly ILoggerWrapper _logger;
        private readonly CloudTable _table;

        public TableStateRepository(CacheConfiguration configuration, ILoggerWrapper logger)
        {
            _logger = logger;

            var storageAccount = CloudStorageAccount.Parse(configuration.TableStorageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(configuration.StateTableName);
        }
        
        public async Task<DateTime> GetLastStagingDateClearedAsync(string entityType, CancellationToken cancellationToken)
        {
            var partitionKey = $"{entityType.ToLower()}-staging";

            return await GetLastDateTimeStateAsync(partitionKey, "last-cleared", new DateTime(2020, 6, 1), cancellationToken);
        }

        public async Task SetLastStagingDateClearedAsync(string entityType, DateTime lastRead, CancellationToken cancellationToken)
        {
            var partitionKey = $"{entityType.ToLower()}-staging";
            
            await SetLastDateTimeStateAsync(partitionKey, "last-cleared", lastRead, cancellationToken);
        }


        private async Task<DateTime> GetLastDateTimeStateAsync(
            string partitionKey,
            string rowKey,
            DateTime defaultValue,
            CancellationToken cancellationToken)
        {
            await _table.CreateIfNotExistsAsync(cancellationToken);

            var operation = TableOperation.Retrieve<LastDateTimeEntity>(partitionKey, rowKey);
            var operationResult = await _table.ExecuteAsync(operation, cancellationToken);
            var entity = (LastDateTimeEntity) operationResult.Result;

            if (entity == null)
            {
                return defaultValue;
            }

            return entity.LastRead;
        }

        private async Task SetLastDateTimeStateAsync(
            string partitionKey,
            string rowKey,
            DateTime lastRead,
            CancellationToken cancellationToken)
        {
            await _table.CreateIfNotExistsAsync(cancellationToken);

            var operation = TableOperation.InsertOrReplace(new LastDateTimeEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                LastRead = lastRead,
            });
            await _table.ExecuteAsync(operation, cancellationToken);
        }
    }
}