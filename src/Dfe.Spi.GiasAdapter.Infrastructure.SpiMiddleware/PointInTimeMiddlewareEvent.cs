using System;

namespace Dfe.Spi.GiasAdapter.Infrastructure.SpiMiddleware
{
    public class PointInTimeMiddlewareEvent<T>
    {
        public T Details { get; set; }
        public DateTime PointInTime { get; set; }
    }
}