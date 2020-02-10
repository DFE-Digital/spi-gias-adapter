using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public class TableEstablishmentRepository : IEstablishmentRepository
    {
        private readonly ILoggerWrapper _logger;
        private readonly CloudTable _table;

        public TableEstablishmentRepository(CacheConfiguration configuration, ILoggerWrapper logger)
        {
            _logger = logger;

            var storageAccount = CloudStorageAccount.Parse(configuration.TableStorageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(configuration.EstablishmentTableName);
        }

        
        
        public async Task StoreAsync(Establishment establishment, CancellationToken cancellationToken)
        {
            await _table.CreateIfNotExistsAsync(cancellationToken);

            var operation = TableOperation.InsertOrReplace(ModelToCurrent(establishment));
            await _table.ExecuteAsync(operation, cancellationToken);
        }

        public async Task StoreInStagingAsync(Establishment[] establishments, CancellationToken cancellationToken)
        {
            const int batchSize = 100;

            await _table.CreateIfNotExistsAsync(cancellationToken);

            var partitionedEntities = establishments
                .Select(ModelToStaging)
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

                    _logger.Debug(
                        $"Inserting {position} to {partition.Length} for partition {entities.First().PartitionKey}");
                    await _table.ExecuteBatchAsync(batch, cancellationToken);

                    position += batchSize;
                }
            }
        }

        public async Task<Establishment> GetEstablishmentAsync(long urn, CancellationToken cancellationToken)
        {
            var operation = TableOperation.Retrieve<EstablishmentEntity>(urn.ToString(), "current");
            var operationResult = await _table.ExecuteAsync(operation, cancellationToken);
            var entity = (EstablishmentEntity) operationResult.Result;

            if (entity == null)
            {
                return null;
            }

            return EntityToModel(entity);
        }

        public async Task<Establishment> GetEstablishmentFromStagingAsync(long urn, CancellationToken cancellationToken)
        {
            var operation = TableOperation.Retrieve<EstablishmentEntity>(GetStagingPartitionKey(urn),urn.ToString());
            var operationResult = await _table.ExecuteAsync(operation, cancellationToken);
            var entity = (EstablishmentEntity) operationResult.Result;

            if (entity == null)
            {
                return null;
            }

            return EntityToModel(entity);
        }


        
        

        private EstablishmentEntity ModelToCurrent(Establishment establishment)
        {
            return ModelToEntity(establishment.Urn.ToString(), "current", establishment);
        }

        private EstablishmentEntity ModelToStaging(Establishment establishment)
        {
            return ModelToEntity(GetStagingPartitionKey(establishment.Urn), establishment.Urn.ToString(), establishment);
        }

        private EstablishmentEntity ModelToEntity(string partitionKey, string rowKey, Establishment establishment)
        {
            return new EstablishmentEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Urn = establishment.Urn,
                Name = establishment.EstablishmentName,
                Ukprn = establishment.Ukprn,
                CompaniesHouseNumber = establishment.CompaniesHouseNumber,
                CharitiesCommissionNumber = establishment.CharitiesCommissionNumber,
                AcademyTrustCode = establishment.Trusts.Code?.ToString(),
                LocalAuthorityCode = establishment.LA.Code?.ToString(),
                EstablishmentNumber = establishment.EstablishmentNumber?.ToString(),
                PreviousEstablishmentNumber = establishment.PreviousEstablishmentNumber?.ToString(),
                Postcode = establishment.Postcode,
            };
        }

        private Establishment EntityToModel(EstablishmentEntity entity)
        {
            return new Establishment
            {
                Urn = entity.Urn,
                EstablishmentName = entity.Name,
                Ukprn = entity.Ukprn,
                CompaniesHouseNumber = entity.CompaniesHouseNumber,
                CharitiesCommissionNumber = entity.CharitiesCommissionNumber,
                Trusts = this.CreateCodeNamePair(entity.AcademyTrustCode),
                LA = this.CreateCodeNamePair(entity.LocalAuthorityCode),
                EstablishmentNumber = this.ParseNullable(entity.EstablishmentNumber),
                PreviousEstablishmentNumber = this.ParseNullable(entity.PreviousEstablishmentNumber),
                Postcode = entity.Postcode,
            };
        }

        private CodeNamePair CreateCodeNamePair(string value)
        {
            CodeNamePair toReturn = new CodeNamePair()
            {
                Code = this.ParseNullable(value),
            };

            return toReturn;
        }

        private int? ParseNullable(string value)
        {
            int? toReturn = null;

            int parsedCode;
            if (int.TryParse(value, out parsedCode))
            {
                toReturn = parsedCode;
            }

            return toReturn;
        }
        
        private string GetStagingPartitionKey(long urn)
        {
            return $"staging{Math.Floor(urn / 5000d) * 5000}";
        }
    }
}