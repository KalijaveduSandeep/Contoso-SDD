using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IScanQueueService
{
    Task EnqueueScanAsync(DocumentScanJob job, CancellationToken cancellationToken = default);
}
