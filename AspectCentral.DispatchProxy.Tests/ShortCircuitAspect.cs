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
    /// <see cref="AspectContext.ReturnValue" /> to a <see cref="Task.Delay(int)" /> Task
    /// that is observably pending for a measurable window. <c>Task.Delay</c> is timer-backed
    /// and cannot complete until at least its delay has elapsed, so the Task is guaranteed
    /// to still be pending when <c>BaseAspect.HandleAsyncShortCircuit</c> attaches its
    /// <see cref="Task.ContinueWith(System.Action{Task})" /> continuation (which is
    /// configured with <c>ExecuteSynchronously</c>). That forces the continuation to be
    /// scheduled rather than running inline at registration time on an already-completed
    /// task, and unambiguously exercises the non-generic short-circuit branch.
    /// <para>
    /// <see cref="Task.Run(System.Action)" />, <see cref="Task.CompletedTask" />, and a
    /// <see cref="TaskCompletionSource" /> completed from a thread-pool worker are all
    /// deliberately avoided here: each can be observably complete by the time
    /// <c>ContinueWith</c> is reached, allowing the continuation to run inline and
    /// reintroducing the original coverage-attribution flake.
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

        // Hand BaseAspect a Task that is observably pending for a measurable window
        // (Task.Delay returns a timer-backed Task that cannot complete until at least
        // its delay has elapsed). This guarantees the Task is still pending when
        // BaseAspect.HandleAsyncShortCircuit attaches its ContinueWith continuation,
        // even though that continuation is configured with ExecuteSynchronously, so the
        // continuation cannot run inline at registration time.
        //
        // Task.Run / Task.CompletedTask / a TaskCompletionSource completed from a
        // thread-pool worker can all be observably complete by the time ContinueWith
        // runs and would reintroduce the inline-execution race.
        aspectContext.ReturnValue = Task.Delay(50);
    }

    /// <inheritdoc />
    public override void PostInvoke(AspectContext aspectContext)
    {
        PostInvokeRan = true;
    }
}
