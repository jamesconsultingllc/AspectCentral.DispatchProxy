//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="ShortCircuitAspect.cs" company="James Consulting LLC">
//    Copyright (c) 2026 James Consulting LLC. Licensed under the MIT License.
//  </copyright>
//  ----------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Test aspect that short-circuits intercepted calls without supplying a generic
/// <see cref="Task{TResult}" /> result. Used to exercise the sync short-circuit branch
/// and the non-generic <see cref="Task" /> short-circuit branch of
/// <c>BaseAspect&lt;T&gt;.DispatchIntercepted</c> / <c>HandleAsyncShortCircuit</c>.
/// </summary>
/// <typeparam name="T">The interface type being proxied.</typeparam>
public class ShortCircuitAspect<T> : BaseAspect<T> where T : class?
{
    /// <summary>
    /// When non-<see langword="null" />, <see cref="PreInvoke" /> assigns this task to
    /// <see cref="AspectContext.ReturnValue" /> to exercise the non-generic Task short-circuit
    /// branch of <c>BaseAspect.HandleAsyncShortCircuit</c>. Use a <see cref="TaskCompletionSource" />
    /// (non-generic) to obtain a non-generic <see cref="Task" />; <c>Task.CompletedTask</c> is
    /// internally a <c>Task&lt;VoidTaskResult&gt;</c> and would route through the generic branch.
    /// When <see langword="null" />, <see cref="PreInvoke" /> leaves <c>ReturnValue</c> at its
    /// default to exercise the sync short-circuit / fallback <c>PostInvoke</c> path.
    /// </summary>
    public static Task? SuppliedTask { get; set; }

    /// <summary>Records that PostInvoke ran.</summary>
    public static bool PostInvokeRan { get; set; }

    /// <summary>Creates a configured short-circuit proxy.</summary>
    public static T Create(T instance, Type type, ILoggerFactory loggerFactory,
        IAspectConfigurationProvider provider)
    {
        object proxy = Create<T, ShortCircuitAspect<T>>()!;
        var typed = (ShortCircuitAspect<T>)proxy;
        typed.Instance = instance;
        typed.ObjectType = type;
        typed.AspectConfigurationProvider = provider;
        typed.Logger = loggerFactory.CreateLogger(type.FullName!);
        typed.FactoryType = TestAspectFactory.Type;
        return (T)proxy;
    }

    /// <inheritdoc />
    public override void PreInvoke(AspectContext aspectContext)
    {
        aspectContext.InvokeMethod = false;
        if (SuppliedTask is not null) aspectContext.ReturnValue = SuppliedTask;
    }

    /// <inheritdoc />
    public override void PostInvoke(AspectContext aspectContext)
    {
        PostInvokeRan = true;
    }
}
