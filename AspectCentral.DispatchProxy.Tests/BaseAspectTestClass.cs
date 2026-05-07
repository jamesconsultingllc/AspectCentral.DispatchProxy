//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="BaseAspectTestClass.cs" company="James Consulting LLC">
//    Copyright (c) 2019 James Consulting LLC. Licensed under the MIT License.
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using JamesConsulting.Threading;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Test aspect that short-circuits intercepted calls by supplying a synthetic return value.
/// </summary>
/// <typeparam name="T">
/// The interface type being proxied.
/// </typeparam>
public class BaseAspectTestClass<T> : BaseAspect<T> where T : class?
{
    /// <summary>
    /// Creates a configured proxy instance for exercising <see cref="BaseAspect{T}" /> behavior.
    /// </summary>
    /// <param name="instance">
    /// The service instance to wrap.
    /// </param>
    /// <param name="type">
    /// The concrete implementation type behind <paramref name="instance" />.
    /// </param>
    /// <param name="loggerFactory">
    /// The logger factory used by the proxy.
    /// </param>
    /// <param name="inMemoryAspectConfigurationProvider">
    /// The configuration provider consulted by the proxy.
    /// </param>
    /// <returns>
    /// A proxy implementing <typeparamref name="T" />.
    /// </returns>
    public static T Create(T instance, Type type, ILoggerFactory loggerFactory,
        IAspectConfigurationProvider inMemoryAspectConfigurationProvider)
    {
        object proxy = Create<T, BaseAspectTestClass<T>>()!;
        ((BaseAspectTestClass<T>)proxy).Instance = instance;
        ((BaseAspectTestClass<T>)proxy).ObjectType = type;
        ((BaseAspectTestClass<T>)proxy).AspectConfigurationProvider = inMemoryAspectConfigurationProvider;
        ((BaseAspectTestClass<T>)proxy).Logger = loggerFactory.CreateLogger(type.FullName!);
        ((BaseAspectTestClass<T>)proxy).FactoryType = TestAspectFactory.Type;
        return (T)proxy;
    }

    /// <summary>
    /// Logs when post-invocation logic unexpectedly runs for this short-circuiting test aspect.
    /// </summary>
    /// <param name="aspectContext">
    /// The invocation context for the completed method call.
    /// </param>
    public override void PostInvoke(AspectContext aspectContext)
    {
        Logger.LogInformation("Should not be invoked");
    }

    /// <summary>
    /// Supplies a synthetic task result and prevents the underlying method from running.
    /// </summary>
    /// <param name="aspectContext">
    /// The invocation context to modify before dispatch.
    /// </param>
    public override void PreInvoke(AspectContext aspectContext)
    {
        Logger.LogInformation("Setting result");
        aspectContext.ReturnValue = aspectContext.TargetMethod.CreateTaskResult(new MyUnitTestClass(12, "testing 123"));
        aspectContext.InvokeMethod = false;
    }
}
