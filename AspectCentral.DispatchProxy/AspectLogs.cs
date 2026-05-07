using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Logging;

/// <summary>
/// Source-generated log messages for the DispatchProxy library.
/// </summary>
internal static partial class AspectLogs
{
    /// <summary>
    /// Logs that a type's public methods were added to the fallback method cache.
    /// </summary>
    /// <param name="logger">The logger that receives the event.</param>
    /// <param name="typeName">The full name of the type whose methods were cached.</param>
    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Added {TypeName} methods to cache")]
    public static partial void AddedMethodsToCache(ILogger logger, string typeName);

    /// <summary>
    /// Logs that an <see cref="AspectCentral.Abstractions.AspectContext" /> was created for an invocation.
    /// </summary>
    /// <param name="logger">The logger that receives the event.</param>
    [LoggerMessage(EventId = 1002, Level = LogLevel.Debug, Message = "AspectContext generated")]
    public static partial void AspectContextGenerated(ILogger logger);

    /// <summary>
    /// Logs that a method is being invoked with aspect interception enabled.
    /// </summary>
    /// <param name="logger">The logger that receives the event.</param>
    /// <param name="invocationString">The formatted method invocation string.</param>
    [LoggerMessage(EventId = 1003, Level = LogLevel.Debug, Message = "Invoking {InvocationString}")]
    public static partial void InvokingWithInterception(ILogger logger, string invocationString);

    /// <summary>
    /// Logs that a method is being invoked without running aspect hooks.
    /// </summary>
    /// <param name="logger">The logger that receives the event.</param>
    /// <param name="invocationString">The formatted method invocation string.</param>
    [LoggerMessage(EventId = 1004, Level = LogLevel.Debug,
        Message = "Invoking {InvocationString} without interception")]
    public static partial void InvokingWithoutInterception(ILogger logger, string invocationString);

    /// <summary>
    /// Logs the start of a logging-aspect invocation.
    /// </summary>
    /// <param name="logger">The logger that receives the event.</param>
    /// <param name="invocationString">The formatted method invocation string.</param>
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "{InvocationString} Start")]
    public static partial void LoggingAspectStart(ILogger logger, string invocationString);

    /// <summary>
    /// Logs the completion of a logging-aspect invocation.
    /// </summary>
    /// <param name="logger">The logger that receives the event.</param>
    /// <param name="invocationString">The formatted method invocation string.</param>
    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "{InvocationString} End")]
    public static partial void LoggingAspectEnd(ILogger logger, string invocationString);

    /// <summary>
    /// Logs the return value produced by an intercepted method.
    /// </summary>
    /// <param name="logger">The logger that receives the event.</param>
    /// <param name="returnValue">The value returned by the intercepted method.</param>
    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Return value : {ReturnValue}")]
    public static partial void LoggingAspectReturnValue(ILogger logger, object? returnValue);

    /// <summary>
    /// Logs the start of a profiling-aspect stopwatch.
    /// </summary>
    /// <param name="logger">The logger that receives the event.</param>
    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Starting Stopwatch")]
    public static partial void ProfilingAspectStart(ILogger logger);

    /// <summary>
    /// Logs the elapsed duration measured by the profiling aspect.
    /// </summary>
    /// <param name="logger">The logger that receives the event.</param>
    /// <param name="hours">Elapsed hours.</param>
    /// <param name="minutes">Elapsed minutes.</param>
    /// <param name="seconds">Elapsed seconds.</param>
    /// <param name="milliseconds">Elapsed milliseconds.</param>
    [LoggerMessage(EventId = 3002, Level = LogLevel.Information,
        Message = "Runtime {Hours:00}:{Minutes:00}:{Seconds:00}.{Milliseconds:000}")]
    public static partial void
        ProfilingAspectEnd(ILogger logger, int hours, int minutes, int seconds, int milliseconds);
}
