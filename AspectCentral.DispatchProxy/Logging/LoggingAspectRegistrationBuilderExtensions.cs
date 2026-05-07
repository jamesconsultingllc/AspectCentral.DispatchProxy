// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingAspectRegistrationBuilderExtensions.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The logging aspect registration builder extensions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Reflection;
using AspectCentral.Abstractions;

namespace AspectCentral.DispatchProxy.Logging;

/// <summary>
/// Fluent extensions that register the built-in <see cref="LoggingAspect{T}" /> against an
/// <see cref="IAspectRegistrationBuilder" />. The logging aspect emits "Start" / "End" / return
/// value entries at <see cref="Microsoft.Extensions.Logging.LogLevel.Information" /> via the
/// source-generated log methods (event IDs 2001/2002/2003).
/// </summary>
public static class LoggingAspectRegistrationBuilderExtensions
{
    /// <summary>
    /// Registers the built-in <see cref="LoggingAspectFactory" /> with the supplied builder so
    /// that the logging aspect is added to every interface-based service the builder knows
    /// about (or, when <paramref name="methodsToIntercept" /> is non-empty, only those methods).
    /// </summary>
    /// <param name="aspectRegistrationBuilder">
    /// The fluent aspect registration builder. Must not be <see langword="null" />.
    /// </param>
    /// <param name="methodsToIntercept">
    /// Optional method-level filter. When empty, every method on the affected interfaces is
    /// logged; otherwise only the listed methods produce log entries and other calls pass
    /// through to the underlying implementation without interception.
    /// </param>
    /// <returns>The same <see cref="IAspectRegistrationBuilder" /> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="aspectRegistrationBuilder" /> is <see langword="null" />.
    /// </exception>
    public static IAspectRegistrationBuilder AddLoggingAspect(this IAspectRegistrationBuilder aspectRegistrationBuilder,
        params MethodInfo[] methodsToIntercept)
    {
        if (aspectRegistrationBuilder == null) throw new ArgumentNullException(nameof(aspectRegistrationBuilder));

        aspectRegistrationBuilder.AddAspect(LoggingAspectFactory.LoggingAspectFactoryType,
            methodsToIntercept: methodsToIntercept);
        return aspectRegistrationBuilder;
    }
}