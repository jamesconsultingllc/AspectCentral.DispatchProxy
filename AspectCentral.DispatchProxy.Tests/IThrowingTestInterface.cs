namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Test fixture interface whose methods deliberately throw, to exercise the error-path branches
/// of <see cref="BaseAspect{T}" /> (sync catch, async faulted task, async cancellation).
/// </summary>
public interface IThrowingTestInterface
{
    /// <summary>
    /// Throws synchronously when invoked.
    /// </summary>
    void ThrowSync();

    /// <summary>
    /// Returns a faulted task when invoked.
    /// </summary>
    /// <returns>A faulted task.</returns>
    Task ThrowAsync();

    /// <summary>
    /// Returns a canceled task when invoked.
    /// </summary>
    /// <returns>A canceled task.</returns>
    Task ReturnCanceledTask();
}

/// <summary>
/// Throwing implementation used to exercise synchronous, faulted-task, and canceled-task telemetry.
/// </summary>
public sealed class ThrowingTestInterface : IThrowingTestInterface
{
    /// <summary>
    /// Cached <see cref="Type" /> token for this throwing test implementation.
    /// </summary>
    public static readonly Type Type = typeof(ThrowingTestInterface);

    /// <inheritdoc />
    public void ThrowSync()
    {
        throw new InvalidOperationException("sync boom");
    }

    /// <inheritdoc />
    public Task ThrowAsync()
    {
        return Task.FromException(new InvalidOperationException("async boom"));
    }

    /// <inheritdoc />
    public Task ReturnCanceledTask()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return Task.FromCanceled(cts.Token);
    }
}
