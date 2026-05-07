using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// A minimal <see cref="BaseAspect{T}" /> subclass that does not override <c>PreInvoke</c> or
/// <c>PostInvoke</c>. Used to exercise the empty virtual defaults on <see cref="BaseAspect{T}" />
/// that the production aspects always override.
/// </summary>
public class PassThroughAspect<T> : BaseAspect<T> where T : class?
{
    /// <summary>
    /// Open generic <see cref="Type" /> token for this pass-through test aspect.
    /// </summary>
    public static readonly Type Type = typeof(PassThroughAspect<>);

    /// <summary>
    /// Creates a proxy that delegates directly to the supplied service instance.
    /// </summary>
    /// <param name="instance">The service instance to wrap.</param>
    /// <param name="type">The concrete implementation type behind <paramref name="instance" />.</param>
    /// <param name="loggerFactory">The logger factory used by the proxy.</param>
    /// <param name="provider">The aspect configuration provider consulted by the proxy.</param>
    /// <returns>A pass-through proxy implementing <typeparamref name="T" />.</returns>
    public static T Create(T instance, Type type, ILoggerFactory loggerFactory, IAspectConfigurationProvider provider)
    {
        object proxy = Create<T, PassThroughAspect<T>>()!;
        var aspect = (PassThroughAspect<T>)proxy;
        aspect.Instance = instance;
        aspect.ObjectType = type;
        aspect.AspectConfigurationProvider = provider;
        aspect.Logger = loggerFactory.CreateLogger(type.FullName!);
        aspect.FactoryType = Type;
        return (T)proxy;
    }
}
