using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

#nullable enable annotations

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
///     A minimal <see cref="BaseAspect{T}"/> subclass that does not override <c>PreInvoke</c> or
///     <c>PostInvoke</c>. Used to exercise the empty virtual defaults on <see cref="BaseAspect{T}"/>
///     that the production aspects always override.
/// </summary>
public class PassThroughAspect<T> : BaseAspect<T> where T : class?
{
    public static readonly Type Type = typeof(PassThroughAspect<>);

    public static T Create(T instance, Type type, ILoggerFactory loggerFactory, IAspectConfigurationProvider provider)
    {
        object proxy = Create<T, PassThroughAspect<T>>();
        var aspect = (PassThroughAspect<T>)proxy;
        aspect.Instance = instance;
        aspect.ObjectType = type;
        aspect.AspectConfigurationProvider = provider;
        aspect.Logger = loggerFactory.CreateLogger(type.FullName!);
        aspect.FactoryType = Type;
        return (T)proxy;
    }
}
