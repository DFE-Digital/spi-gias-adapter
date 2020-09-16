using System;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public class StagingBatchQueueItem<TParentIdentifier>
    {
        public TParentIdentifier ParentIdentifier { get; set; }
        public long[] Urns { get; set; }
        public DateTime PointInTime { get; set; }
    }
}