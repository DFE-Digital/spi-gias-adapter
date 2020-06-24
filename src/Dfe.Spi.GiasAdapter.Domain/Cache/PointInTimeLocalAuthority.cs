using System;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public class PointInTimeLocalAuthority : LocalAuthority
    {
        public DateTime PointInTime { get; set; }
        public bool IsCurrent { get; set; }
    }
}