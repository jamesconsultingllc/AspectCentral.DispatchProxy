// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfilingAspectFactory.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The profiling aspect factory.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Profiling;

/// <summary>
/// <see cref="IAspectFactory" /> that produces <see cref="ProfilingAspect{T}" /> proxies. Discovered
/// and registered automatically by
/// <see cref="ServiceCollectionExtensions.AddAspectSupport(Microsoft.Extensions.DependencyInjection.IServiceCollection)" />
/// .
/// </summary>
/// <param name="loggerFactory">Used to create a categorized logger named after the implementation type.</param>
/// <param name="aspectConfigurationProvider">Consulted on every call to decide whether the call should be timed.</param>
public class ProfilingAspectFactory(
    ILoggerFactory loggerFactory,
    IAspectConfigurationProvider aspectConfigurationProvider)
    : BaseAspectFactory(loggerFactory, aspectConfigurationProvider)
{
    /// <summary>
    /// Cached <see cref="Type" /> token for <see cref="ProfilingAspectFactory" />. Used by
    /// <see cref="ProfilingAspectRegistrationBuilderExtensions.AddProfilingAspect" /> to avoid
    /// repeated <c>typeof</c> evaluations.
    /// </summary>
    public static readonly Type ProfilingAspectFactoryType = typeof(ProfilingAspectFactory);

    /// <inheritdoc />
    public override T Create<T>(T instance, Type implementationType)
    {
        return ProfilingAspect<T>.Create(instance, implementationType, LoggerFactory, AspectConfigurationProvider,
            ProfilingAspectFactoryType);
    }
}