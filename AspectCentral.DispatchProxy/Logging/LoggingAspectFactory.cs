// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingAspectFactory.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The logging aspect factory.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Logging;

/// <summary>
///     <see cref="IAspectFactory"/> that produces <see cref="LoggingAspect{T}"/> proxies. Discovered
///     and registered automatically by
///     <see cref="ServiceCollectionExtensions.AddAspectSupport(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>.
/// </summary>
/// <param name="loggerFactory">Used to create a categorized logger named after the implementation type.</param>
/// <param name="aspectConfigurationProvider">Consulted on every call to decide whether to log it.</param>
public class LoggingAspectFactory(ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider) : BaseAspectFactory(loggerFactory, aspectConfigurationProvider)
{
    /// <summary>
    ///     Cached <see cref="Type"/> token for <see cref="LoggingAspectFactory"/>. Used by
    ///     <see cref="LoggingAspectRegistrationBuilderExtensions.AddLoggingAspect"/> to avoid repeated
    ///     <c>typeof</c> evaluations.
    /// </summary>
    public static readonly Type LoggingAspectFactoryType = typeof(LoggingAspectFactory);

    /// <inheritdoc />
    public override T Create<T>(T instance, Type implementationType)
    {
        return LoggingAspect<T>.Create(instance, implementationType, LoggerFactory, AspectConfigurationProvider, LoggingAspectFactoryType);
    }
}