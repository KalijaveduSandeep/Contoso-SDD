using System.Text.Json;
using Azure.Storage.Queues;
using Azure;
using ContosoDashboard.Models;
using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public class ScanQueueOptions
{
    public string? ConnectionString { get; set; }
    public string QueueName { get; set; } = "document-scan-jobs";
}

public class ScanQueueService : IScanQueueService
{
    private readonly ScanQueueOptions _options;
    private readonly ILogger<ScanQueueService> _logger;

    public ScanQueueService(IOptions<ScanQueueOptions> options, ILogger<ScanQueueService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnqueueScanAsync(DocumentScanJob job, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _logger.LogError("Queue connection is not configured.");
            throw new InvalidOperationException("Queue/scanning pipeline unavailable.");
        }

        var clientOptions = new QueueClientOptions
        {
            Retry =
            {
                MaxRetries = 1,
                Delay = TimeSpan.FromMilliseconds(250),
                MaxDelay = TimeSpan.FromSeconds(1)
            }
        };

        try
        {
            var client = new QueueClient(_options.ConnectionString, _options.QueueName, clientOptions);
            await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var payload = JsonSerializer.Serialize(job);
            await client.SendMessageAsync(payload, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to enqueue scan job for document {DocumentId}.", job.DocumentId);
            throw new InvalidOperationException(
                "Queue/scanning pipeline unavailable. Start Azurite queue service on localhost:10001 or update ScanQueue:ConnectionString.",
                ex);
        }
    }
}
