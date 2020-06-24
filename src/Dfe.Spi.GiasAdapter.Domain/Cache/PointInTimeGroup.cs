using System;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public class PointInTimeGroup : Group
    {
        public DateTime PointInTime { get; set; }
        public bool IsCurrent { get; set; }
    }
}