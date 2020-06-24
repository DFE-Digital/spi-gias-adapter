using System;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public class StagingBatchQueueItem<TIdentifier>
    {
        public TIdentifier[] Identifiers { get; set; }
        public DateTime PointInTime { get; set; }
    }
}