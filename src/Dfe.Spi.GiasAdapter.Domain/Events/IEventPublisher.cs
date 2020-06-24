using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Models.Entities;

namespace Dfe.Spi.GiasAdapter.Domain.Events
{
    public interface IEventPublisher
    {
        Task PublishLearningProviderCreatedAsync(LearningProvider learningProvider, DateTime pointInTime, CancellationToken cancellationToken);
        Task PublishLearningProviderUpdatedAsync(LearningProvider learningProvider, DateTime pointInTime, CancellationToken cancellationToken);
        
        Task PublishManagementGroupCreatedAsync(ManagementGroup managementGroup, DateTime pointInTime, CancellationToken cancellationToken);
        Task PublishManagementGroupUpdatedAsync(ManagementGroup managementGroup, DateTime pointInTime, CancellationToken cancellationToken);
    }
}