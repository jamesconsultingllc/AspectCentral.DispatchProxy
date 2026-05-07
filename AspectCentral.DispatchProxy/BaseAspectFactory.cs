//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="BaseAspectFactory.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy;

/// <summary>
/// Convenience base class for <see cref="IAspectFactory" /> implementations.
/// </summary>
/// <remarks>
///     <para>
///     Holds the two collaborators every aspect needs: an <see cref="ILoggerFactory" /> for emitting
///     diagnostic output from the aspect, and an <see cref="IAspectConfigurationProvider" /> consulted
///     on each call to decide whether interception should run.
///     </para>
///     <para>
///     Derive from this class rather than implementing <see cref="IAspectFactory" /> directly so the
///     <see
///         cref="ServiceCollectionExtensions.AddAspectSupport(Microsoft.Extensions.DependencyInjection.IServiceCollection)" />
///     scanner can discover and register the factory automatically.
///     </para>
/// </remarks>
/// <param name="loggerFactory">
/// Used by the produced aspect to obtain a categorized
/// <see cref="Microsoft.Extensions.Logging.ILogger" />.
/// </param>
/// <param name="aspectConfigurationProvider">Consulted on every call to determine if interception should run.</param>
    /// <exception cref="ArgumentNullException">Thrown if either constructor argument is <see langword="null" />.</exception>
public abstract class BaseAspectFactory(
    ILoggerFactory loggerFactory,
    IAspectConfigurationProvider aspectConfigurationProvider) : IAspectFactory
{
    /// <summary>
    /// <see cref="ILoggerFactory" /> used by produced aspects to create a categorized logger
    /// for the implementation type being wrapped.
    /// </summary>
    protected readonly ILoggerFactory LoggerFactory =
        loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    /// <summary>
    /// <see cref="IAspectConfigurationProvider" /> the produced aspect consults on every
    /// intercepted call to decide whether <c>PreInvoke</c>/<c>PostInvoke</c> hooks should run.
    /// </summary>
    protected readonly IAspectConfigurationProvider AspectConfigurationProvider = aspectConfigurationProvider ??
                                                                                  throw new ArgumentNullException(nameof(aspectConfigurationProvider));

    /// <inheritdoc />
    public abstract T Create<T>(T instance, Type implementationType) where T : class?;
}
