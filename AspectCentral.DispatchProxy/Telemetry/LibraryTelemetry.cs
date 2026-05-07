using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AspectCentral.DispatchProxy.Telemetry;

/// <summary>
/// Named ActivitySource and Meter for the library.
/// Consumers register these with their OTel pipeline:
/// .WithTracing(b => b.AddSource(LibraryActivitySources.Aspects))
/// .WithMetrics(b => b.AddMeter(LibraryMeters.Aspects))
/// </summary>
public static class LibraryActivitySources
{
    /// <summary>Tracing source for aspect operations.</summary>
    public const string Aspects = "AspectCentral.DispatchProxy.Aspects";

    /// <summary>All sources — for easy bulk registration by consumers.</summary>
    public static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[] { Aspects });

    /// <summary>The shared ActivitySource instance.</summary>
    internal static readonly ActivitySource ActivitySource = new(Aspects);
}

/// <summary>
/// Named Meter for the library.
/// </summary>
public static class LibraryMeters
{
    /// <summary>Meter for aspect metrics.</summary>
    public const string Aspects = "AspectCentral.DispatchProxy.Aspects";

    /// <summary>All meters — for easy bulk registration by consumers.</summary>
    public static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[] { Aspects });

    /// <summary>The shared Meter instance.</summary>
    internal static readonly Meter Meter = new(Aspects);

    /// <summary>Counter for total invocations.</summary>
    internal static readonly Counter<long> InvocationsCounter =
        Meter.CreateCounter<long>("aspect.invocations", null, "Number of aspect invocations");

    /// <summary>Histogram for method duration in milliseconds.</summary>
    internal static readonly Histogram<double> DurationHistogram =
        Meter.CreateHistogram<double>("aspect.invocation_duration", "ms", "Duration of aspect invocations");
}