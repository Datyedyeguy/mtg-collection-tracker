using System;
using System.Threading;
using System.Threading.Channels;

namespace MTGCollectionTracker.Api.Services;

/// <summary>
/// In-memory queue that connects the <c>ImportsController</c> (producer) to the
/// <c>ImportWorkerService</c> (consumer).
///
/// Backed by an unbounded <see cref="Channel{T}"/>. The channel is in-memory, but
/// that is safe because:
/// <list type="bullet">
///   <item>Job records are written to the database <em>before</em> their IDs are enqueued.</item>
///   <item><see cref="ImportWorkerService"/> re-scans for Pending/Processing jobs on startup,
///         so any IDs lost during a restart are automatically re-enqueued.</item>
/// </list>
/// Registered as a singleton so that both the controller and the hosted service share the same instance.
/// </summary>
public interface IImportJobQueue
{
    /// <summary>Enqueue a job ID for background processing. Never blocks.</summary>
    void Enqueue(Guid jobId);

    /// <summary>
    /// Expose the underlying channel reader so the worker can await new items
    /// with proper cancellation support.
    /// </summary>
    ChannelReader<Guid> Reader { get; }
}

/// <inheritdoc />
public sealed class ImportJobQueue : IImportJobQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    /// <inheritdoc />
    public void Enqueue(Guid jobId) => _channel.Writer.TryWrite(jobId);

    /// <inheritdoc />
    public ChannelReader<Guid> Reader => _channel.Reader;
}
