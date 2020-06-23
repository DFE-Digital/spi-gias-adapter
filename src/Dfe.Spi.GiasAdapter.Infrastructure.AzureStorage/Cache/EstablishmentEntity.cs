using System;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public class EstablishmentEntity : TableEntity
    {
        public string Establishment
        {
            get;
            set;
        }
        
        public DateTime PointInTime { get; set; }
        public bool IsCurrent { get; set; }
    }
}