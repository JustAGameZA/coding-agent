using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CodingAgent.Services.Orchestration.Infrastructure.Logging;

public interface IExecutionLogService
{
    /// <summary>
    /// Append a log line for an execution.
    /// </summary>
    Task WriteAsync(Guid executionId, string line, CancellationToken ct = default);

    /// <summary>
    /// Complete the log stream for an execution.
    /// </summary>
    void Complete(Guid executionId);

    /// <summary>
    /// Read the log stream for an execution as an async enumerable (used by SSE endpoint).
    /// </summary>
    IAsyncEnumerable<string> ReadStreamAsync(Guid executionId, CancellationToken ct = default);
}

/// <summary>
/// In-memory channel-based implementation for execution log streaming.
/// Suitable for development; can be swapped with Redis/Kafka-backed implementation later.
/// </summary>
public class ExecutionLogService : IExecutionLogService
{
    private readonly ConcurrentDictionary<Guid, Channel<string>> _channels = new();
    private const int DefaultCapacity = 200;

    private Channel<string> GetOrCreate(Guid executionId)
    {
        return _channels.GetOrAdd(executionId, _ =>
        {
            var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(DefaultCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = false,
                SingleWriter = false
            });
            return channel;
        });
    }

    public async Task WriteAsync(Guid executionId, string line, CancellationToken ct = default)
    {
        var ch = GetOrCreate(executionId);
        await ch.Writer.WriteAsync(line, ct);
    }

    public void Complete(Guid executionId)
    {
        if (_channels.TryGetValue(executionId, out var ch))
        {
            ch.Writer.TryComplete();
        }
    }

    public async IAsyncEnumerable<string> ReadStreamAsync(Guid executionId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var ch = GetOrCreate(executionId);
        var reader = ch.Reader;

        // Send an initial hello
        yield return $"stream-start:{executionId}";

        // Keepalive ticker
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (!ct.IsCancellationRequested)
        {
            while (reader.TryRead(out var item))
            {
                yield return item;
            }

            // Wait for either new data or keepalive tick
            var readTask = reader.WaitToReadAsync(ct).AsTask();
            var tickTask = timer.WaitForNextTickAsync(ct).AsTask();
            var completed = await Task.WhenAny(readTask, tickTask);

            if (completed == tickTask)
            {
                yield return "ping";
                continue;
            }

            if (readTask.IsCompleted)
            {
                // If no more data will ever be available, exit
                var hasMore = readTask.Result;
                if (!hasMore)
                {
                    break;
                }
            }
        }

        yield return $"stream-end:{executionId}";
    }
}
