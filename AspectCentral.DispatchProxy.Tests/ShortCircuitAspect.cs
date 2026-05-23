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
    /// When <see langword="true" />, <see cref="PreInvoke" /> sets
    /// <see cref="AspectContext.ReturnValue" /> to a non-generic <see cref="Task" /> that is
    /// guaranteed to still be pending when <c>BaseAspect.HandleAsyncShortCircuit</c> attaches
    /// its <see cref="Task.ContinueWith(System.Action{Task})" /> continuation. This is what
    /// forces the continuation to be scheduled on a worker (rather than running inline at
    /// registration time on an already-completed task) and unambiguously exercises the
    /// non-generic short-circuit branch.
    /// <para>
    /// A <see cref="TaskCompletionSource" /> is used (rather than <see cref="Task.Run" /> or
    /// <see cref="Task.CompletedTask" />) because both of those can be observably complete by
    /// the time <c>HandleAsyncShortCircuit</c> attaches its continuation, allowing the
    /// continuation to run inline and reintroducing the original coverage-attribution flake.
    /// </para>
    /// When <see langword="false" />, <see cref="PreInvoke" /> leaves ReturnValue at its default
    /// to exercise the sync short-circuit / fallback PostInvoke path.
    /// </summary>
    public static bool SupplyNonGenericTask { get; set; }

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
        if (!SupplyNonGenericTask) return;

        var tcs = new TaskCompletionSource();
        // Complete the task on a thread-pool worker after PreInvoke returns, so the Task
        // is observably pending at the moment BaseAspect.HandleAsyncShortCircuit attaches
        // its ContinueWith continuation. This is what guarantees the continuation is
        // scheduled (rather than running inline at registration time on an already-
        // completed task) and is what the NonGenericTaskShortCircuit coverage test depends
        // on. Task.Run / Task.CompletedTask can both be observably complete by the time
        // ContinueWith runs and would reintroduce the inline-execution race.
        _ = Task.Run(tcs.SetResult);
        aspectContext.ReturnValue = tcs.Task;
    }

    /// <inheritdoc />
    public override void PostInvoke(AspectContext aspectContext)
    {
        PostInvokeRan = true;
    }
}
