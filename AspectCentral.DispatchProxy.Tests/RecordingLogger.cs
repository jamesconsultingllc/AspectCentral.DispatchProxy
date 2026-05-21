// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RecordingLogger.cs" company="James Consulting LLC">
//   Copyright (c) 2019 James Consulting LLC. Licensed under the MIT License.
// </copyright>
// <summary>
//   Test-only ILogger that records each Log call so tests can assert on call counts/levels.
//   Replaces the Moq-based Mock&lt;ILogger&gt; pattern (which relied on It.IsAnyType matchers
//   for the generic ILogger.Log&lt;TState&gt; method) — NSubstitute cannot easily intercept
//   ILogger.Log&lt;TState&gt; because the runtime TState is the internal FormattedLogValues type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Tests;

internal sealed class RecordingLogger : ILogger
{
    public List<RecordedLog> Entries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new RecordedLog(logLevel, eventId, formatter(state, exception), exception));
    }

    public int CountAt(LogLevel level) => Entries.Count(e => e.Level == level);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

internal readonly record struct RecordedLog(LogLevel Level, EventId EventId, string Message, Exception? Exception);
