using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Logging;

/// <summary>
/// Source-generated log messages for the DispatchProxy library.
/// </summary>
internal static partial class AspectLogs
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Added {TypeName} methods to cache")]
    public static partial void AddedMethodsToCache(ILogger logger, string typeName);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Debug, Message = "AspectContext generated")]
    public static partial void AspectContextGenerated(ILogger logger);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Debug, Message = "Invoking {InvocationString}")]
    public static partial void InvokingWithInterception(ILogger logger, string invocationString);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Debug, Message = "Invoking {InvocationString} without interception")]
    public static partial void InvokingWithoutInterception(ILogger logger, string invocationString);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "{InvocationString} Start")]
    public static partial void LoggingAspectStart(ILogger logger, string invocationString);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "{InvocationString} End")]
    public static partial void LoggingAspectEnd(ILogger logger, string invocationString);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Return value : {ReturnValue}")]
    public static partial void LoggingAspectReturnValue(ILogger logger, object? returnValue);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Starting Stopwatch")]
    public static partial void ProfilingAspectStart(ILogger logger);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Runtime {Hours:00}:{Minutes:00}:{Seconds:00}.{Milliseconds:00}")]
    public static partial void ProfilingAspectEnd(ILogger logger, int hours, int minutes, int seconds, int milliseconds);
}