namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
///     Test fixture interface whose methods deliberately throw, to exercise the error-path branches
///     of <see cref="BaseAspect{T}"/> (sync catch, async faulted task, async cancellation).
/// </summary>
public interface IThrowingTestInterface
{
    void ThrowSync();

    Task ThrowAsync();

    Task ReturnCanceledTask();
}

public sealed class ThrowingTestInterface : IThrowingTestInterface
{
    public static readonly Type Type = typeof(ThrowingTestInterface);

    public void ThrowSync() => throw new InvalidOperationException("sync boom");

    public Task ThrowAsync() => Task.FromException(new InvalidOperationException("async boom"));

    public Task ReturnCanceledTask()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return Task.FromCanceled(cts.Token);
    }
}
