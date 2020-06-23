using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Cache
{
    public interface IEstablishmentRepository
    {
        Task StoreAsync(PointInTimeEstablishment establishment, CancellationToken cancellationToken);
        Task StoreAsync(PointInTimeEstablishment[] establishments, CancellationToken cancellationToken);
        Task StoreInStagingAsync(PointInTimeEstablishment[] establishments, CancellationToken cancellationToken);
        Task<PointInTimeEstablishment> GetEstablishmentAsync(long urn, CancellationToken cancellationToken);
        Task<PointInTimeEstablishment> GetEstablishmentFromStagingAsync(long urn, DateTime pointInTime, CancellationToken cancellationToken);
    }
}