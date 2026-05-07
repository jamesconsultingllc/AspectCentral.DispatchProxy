// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfilingAspectRegistrationBuilderExtensions.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The profiling aspect registration builder extensions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Reflection;
using AspectCentral.Abstractions;

namespace AspectCentral.DispatchProxy.Profiling;

/// <summary>
/// Fluent extensions that register the built-in <see cref="ProfilingAspect{T}" /> against an
/// <see cref="IAspectRegistrationBuilder" />. The profiling aspect measures end-to-end method
/// duration with a <see cref="System.Diagnostics.Stopwatch" /> and emits the elapsed time at
/// <see cref="Microsoft.Extensions.Logging.LogLevel.Information" /> via the source-generated
/// log methods (event IDs 3001/3002).
/// </summary>
public static class ProfilingAspectRegistrationBuilderExtensions
{
    /// <summary>
    /// Registers the built-in <see cref="ProfilingAspectFactory" /> with the supplied builder so
    /// that the profiling aspect is added to every interface-based service the builder knows
    /// about (or, when <paramref name="methodsToIntercept" /> is non-empty, only those methods).
    /// </summary>
    /// <param name="aspectRegistrationBuilder">
    /// The fluent aspect registration builder. Must not be <see langword="null" />.
    /// </param>
    /// <param name="methodsToIntercept">
    /// Optional method-level filter. When empty, every method on the affected interfaces is
    /// profiled; otherwise only the listed methods are profiled and other calls pass through to
    /// the underlying implementation without timing overhead.
    /// </param>
    /// <returns>The same <see cref="IAspectRegistrationBuilder" /> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="aspectRegistrationBuilder" /> is <see langword="null" />.
    /// </exception>
    public static IAspectRegistrationBuilder AddProfilingAspect(
        this IAspectRegistrationBuilder aspectRegistrationBuilder, params MethodInfo[] methodsToIntercept)
    {
        if (aspectRegistrationBuilder == null) throw new ArgumentNullException(nameof(aspectRegistrationBuilder));

        aspectRegistrationBuilder.AddAspect(ProfilingAspectFactory.ProfilingAspectFactoryType,
            methodsToIntercept: methodsToIntercept);
        return aspectRegistrationBuilder;
    }
}