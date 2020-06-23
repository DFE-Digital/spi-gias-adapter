using System;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public class PointInTimeEstablishment : Establishment
    {
        public DateTime PointInTime { get; set; }
    }
}