//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="IAspectFactory.cs" company="James Consulting LLC">
//    Copyright (c) 2019 James Consulting LLC. Licensed under the MIT License.
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

namespace AspectCentral.DispatchProxy;

/// <summary>
/// Builds an aspect proxy that wraps a concrete service instance with cross-cutting behavior
/// (logging, profiling, auditing, caching, etc.).
/// </summary>
/// <remarks>
/// Implementations are discovered automatically by
/// <see cref="ServiceCollectionExtensions.AddAspectSupport(Microsoft.Extensions.DependencyInjection.IServiceCollection)" />
/// via reflection over the loaded assemblies and registered as singletons in the DI container.
/// Each implementation is responsible for producing a <see cref="System.Reflection.DispatchProxy" />-derived
/// proxy that forwards calls to the wrapped service instance after running the aspect's hooks.
/// Custom aspects typically inherit from <see cref="BaseAspectFactory" /> rather than implementing this
/// interface directly.
/// </remarks>
public interface IAspectFactory
{
    /// <summary>
    /// Creates a transparent proxy of <typeparamref name="T" /> that wraps <paramref name="instance" />
    /// with this aspect's behavior.
    /// </summary>
    /// <param name="instance">
    /// The inner service instance to wrap. May itself already be a proxy when aspects are chained.
    /// </param>
    /// <param name="implementationType">
    /// The concrete <see cref="Type" /> of the underlying (non-proxied) service. Used by the aspect to
    /// look up attributes, configuration, and the implementation's <see cref="System.Reflection.MethodInfo" />
    /// for each interface call.
    /// </param>
    /// <typeparam name="T">
    /// The interface type being proxied. Must be a reference type (an interface in practice).
    /// </typeparam>
    /// <returns>
    /// A proxy that implements <typeparamref name="T" /> and forwards calls through this aspect.
    /// </returns>
    T Create<T>(T instance, Type implementationType) where T : class?;
}