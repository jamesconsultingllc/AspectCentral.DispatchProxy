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
    /// Captures the runtime type of <see cref="AspectContext.ReturnValue" /> observed by the most
    /// recent <see cref="PostInvoke" /> call. Tests use this to verify that async short-circuit
    /// invocations unwrap <see cref="Task{TResult}" /> into the awaited result before PostInvoke
    /// runs, matching the non-short-circuited path.
    /// </summary>
    public static Type? LastObservedReturnValueType { get; set; }

    /// <summary>
    /// Records the type observed in <see cref="AspectContext.ReturnValue"/> after the
    /// short-circuited async path completes. This is the proof-point for the test:
    /// the proxy must hand <see cref="PostInvoke"/> the unwrapped TResult, not the
    /// outer Task that was placed on ReturnValue in <see cref="PreInvoke"/>.
    /// </summary>
    /// <param name="aspectContext">
    /// The invocation context for the completed method call.
    /// </param>
    public override void PostInvoke(AspectContext aspectContext)
    {
        LastObservedReturnValueType = aspectContext.ReturnValue?.GetType();
        Logger.LogInformation("PostInvoke ran after short-circuited async completion");
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
