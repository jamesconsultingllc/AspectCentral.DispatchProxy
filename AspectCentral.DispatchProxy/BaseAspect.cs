//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="BaseAspect.cs" company="James Consulting LLC">
//    Copyright (c) 2019 James Consulting LLC. Licensed under the MIT License.
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
//    Provides a base implementation for AOP aspects using <see cref="System.Reflection.DispatchProxy"/>.
//    This class handles the complexity of method interception, including support for synchronous
//    and asynchronous (Task, Task&lt;T&gt;) methods, and provides built-in observability.
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy.Logging;
using AspectCentral.DispatchProxy.Telemetry;
using JamesConsulting.Reflection;
using Microsoft.Extensions.Logging;
using MethodTypeOptions = AspectCentral.Abstractions.MethodTypeOptions;

namespace AspectCentral.DispatchProxy;

/// <summary>
/// Provides reusable helpers for invoking asynchronous methods that return <see cref="Task{TResult}" />
/// through an aspect.
/// </summary>
internal static class BaseAspectAsyncProcessor
{
    /// <summary>
    /// Cached <see cref="MethodInfo" /> for <see cref="ProcessFunctionAsync{TK}" /> so callers can
    /// close the generic method for the intercepted return type without using private reflection.
    /// </summary>
    public static readonly MethodInfo ProcessFunctionMethodInfo =
        typeof(BaseAspectAsyncProcessor).GetMethod(
            nameof(ProcessFunctionAsync),
            BindingFlags.Static | BindingFlags.Public)!;

    /// <summary>
    /// Awaits an intercepted <see cref="Task{TResult}" />, stores its completed result in the
    /// supplied <see cref="AspectContext" />, and always runs the aspect's post-invocation hook.
    /// </summary>
    /// <param name="task">The task returned by the intercepted implementation method.</param>
    /// <param name="aspectContext">The invocation context whose return value is updated after completion.</param>
    /// <param name="postInvoke">The post-invocation callback to run after the task completes or faults.</param>
    /// <typeparam name="TK">The result type produced by the intercepted task.</typeparam>
    /// <returns>The result produced by <paramref name="task" />.</returns>
    public static async Task<TK> ProcessFunctionAsync<TK>(
        Task<TK> task,
        AspectContext aspectContext,
        Action<AspectContext> postInvoke)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            aspectContext.ReturnValue = result;
            return result;
        }
        finally
        {
            postInvoke(aspectContext);
        }
    }

    /// <summary>
    /// Awaits a non-generic <see cref="Task" /> short-circuit result and runs
    /// <paramref name="postInvoke" /> in a <c>finally</c>. Returning this task from
    /// <see cref="BaseAspect{T}.HandleAsyncShortCircuit" /> ensures the caller's <c>await</c>
    /// observes <paramref name="postInvoke" /> having completed before resuming, matching the
    /// contract of the generic <see cref="ProcessFunctionAsync{TK}" /> path.
    /// </summary>
    /// <param name="task">The non-generic task supplied by a short-circuiting aspect.</param>
    /// <param name="aspectContext">The invocation context passed to <paramref name="postInvoke" />.</param>
    /// <param name="postInvoke">The post-invocation callback to run after the task completes or faults.</param>
    /// <returns>A task that completes after both <paramref name="task" /> and <paramref name="postInvoke" /> have run.</returns>
    public static async Task ProcessActionAsync(
        Task task,
        AspectContext aspectContext,
        Action<AspectContext> postInvoke)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        finally
        {
            postInvoke(aspectContext);
        }
    }
}

/// <summary>
/// Caches interface-to-implementation method maps used to resolve intercepted interface calls to
/// their concrete implementation methods.
/// </summary>
internal static class BaseAspectMethodMapCache
{
    /// <summary>
    /// Method maps keyed by concrete implementation type and interface type.
    /// </summary>
    private static readonly ConcurrentDictionary<(Type Impl, Type Iface), Dictionary<MethodInfo, MethodInfo>>
        InterfaceMethodMaps = new();

    /// <summary>
    /// Gets an existing method map for the supplied type pair or creates it from
    /// <see cref="Type.GetInterfaceMap(Type)" />.
    /// </summary>
    /// <param name="implType">The concrete implementation type being proxied.</param>
    /// <param name="interfaceType">The interface type that declared the intercepted method.</param>
    /// <returns>A dictionary mapping interface methods to implementation methods.</returns>
    public static Dictionary<MethodInfo, MethodInfo> GetOrAdd(Type implType, Type interfaceType)
    {
        return InterfaceMethodMaps.GetOrAdd((implType, interfaceType), key =>
        {
            var map = key.Impl.GetInterfaceMap(key.Iface);
            var dict = new Dictionary<MethodInfo, MethodInfo>(map.InterfaceMethods.Length);
            for (var i = 0; i < map.InterfaceMethods.Length; i++) dict[map.InterfaceMethods[i]] = map.TargetMethods[i];

            return dict;
        });
    }
}

/// <summary>
/// Base class for implementing AOP aspects using <see cref="System.Reflection.DispatchProxy" />.
/// Provides hooks for pre- and post-invocation logic, supports sync and async methods,
/// and includes built-in observability via OpenTelemetry.
/// </summary>
/// <typeparam name="T">The interface type being proxied.</typeparam>
public abstract class BaseAspect<T> : System.Reflection.DispatchProxy where T : class?
{
    private const string Aspect = "aspect";

    /// <summary>
    /// Gets or sets the type of the factory that created this aspect.
    /// </summary>
    protected Type FactoryType { get; set; } = default!;

    /// <summary>
    /// Gets or sets the provider used to determine if a method should be intercepted.
    /// </summary>
    protected IAspectConfigurationProvider AspectConfigurationProvider { get; set; } = default!;

    /// <summary>
    /// Gets or sets the underlying service instance being wrapped.
    /// </summary>
    protected T Instance { get; set; } = default!;

    /// <summary>
    /// Gets or sets the logger used for diagnostic information.
    /// </summary>
    protected ILogger Logger { get; set; } = default!;

    /// <summary>
    /// Gets or sets the concrete implementation type of the service.
    /// </summary>
    protected Type ObjectType { get; set; } = default!;

    /// <summary>
    /// Generates the context for the current method invocation.
    /// </summary>
    /// <param name="targetMethod">The method being called on the interface.</param>
    /// <param name="args">The arguments passed to the method.</param>
    /// <returns>A new <see cref="AspectContext" /> containing invocation details.</returns>
    public virtual AspectContext GenerateAspectContext(MethodInfo targetMethod, object[] args)
    {
        return new AspectContext(targetMethod, args)
        {
            InvocationString = GenerateMethodNameWithArguments(targetMethod, args, out var implementationMethod),
            InstanceMethod = implementationMethod
        };
    }

    /// <summary>
    /// Resolves the concrete implementation method and generates a descriptive invocation string.
    /// </summary>
    /// <param name="targetMethod">The interface method being invoked.</param>
    /// <param name="args">The method arguments.</param>
    /// <param name="implementationMethod">
    /// Output parameter containing the resolved <see cref="MethodInfo" /> on the
    /// implementation type.
    /// </param>
    /// <returns>A string representation of the method call with its arguments.</returns>
    /// <remarks>
    /// Uses <see cref="Type.GetInterfaceMap" /> rather than <see cref="Type.GetMethods()" /> so that
    /// explicit interface implementations (which are non-public and excluded from
    /// <c>GetMethods()</c>) resolve correctly, and so overload resolution does not depend on
    /// <c>MethodInfo.ToString()</c> string equality.
    /// </remarks>
    public virtual string GenerateMethodNameWithArguments(MethodInfo targetMethod, object[] args,
        out MethodInfo implementationMethod)
    {
        var interfaceType = targetMethod.DeclaringType
                            ?? throw new InvalidOperationException(
                                $"Cannot resolve implementation method for {targetMethod.Name}: targetMethod has no DeclaringType.");

        // DispatchProxy may invoke a closed generic method; map the open generic definition through
        // GetInterfaceMap (which only knows about generic-method-definitions), then re-bind the
        // generic arguments to the closed implementation method.
        var lookupKey = targetMethod.IsGenericMethod
            ? targetMethod.GetGenericMethodDefinition()
            : targetMethod;

        // Guard against non-interface declaring types (e.g. Object methods like ToString/Equals/
        // GetHashCode that some DispatchProxy hosts may route through Invoke) and against
        // implementations that do not implement the declaring interface. GetInterfaceMap throws
        // ArgumentException in both cases, so fall through to the public-methods ToString-based
        // lookup below in those scenarios.
        var canUseInterfaceMap = interfaceType.IsInterface
                                 && interfaceType.IsAssignableFrom(ObjectType);

        MethodInfo? resolved = null;
        if (canUseInterfaceMap)
        {
            var methodMap = BaseAspectMethodMapCache.GetOrAdd(ObjectType, interfaceType);
            methodMap.TryGetValue(lookupKey, out resolved);
        }

        if (resolved == null)
        {
            // Fall back to the legacy ToString-based lookup against the public methods cache for any
            // edge case GetInterfaceMap does not cover (e.g. proxied class with no explicit interface,
            // or Object-declared methods routed through the proxy).
            if (!JamesConsulting.Constants.TypeMethods.ContainsKey(ObjectType))
            {
                AspectLogs.AddedMethodsToCache(Logger, ObjectType.FullName ?? ObjectType.Name);
                JamesConsulting.Constants.TypeMethods[ObjectType] = ObjectType.GetMethods();
            }

            var methodName = lookupKey.ToString();
            resolved = JamesConsulting.Constants.TypeMethods[ObjectType]
                .FirstOrDefault(x => x.ToString() == methodName)
                ?? lookupKey;
        }

        implementationMethod = targetMethod.IsGenericMethod
            ? resolved.MakeGenericMethod(targetMethod.GetGenericArguments())
            : resolved;

        return implementationMethod.ToInvocationString(args);
    }

    /// <summary>
    /// Dispatches the method invocation. Wraps the call in pre/post invocation hooks and telemetry spans.
    /// </summary>
    /// <param name="targetMethod">The method to be invoked.</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>The result of the method invocation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="targetMethod" /> is null.</exception>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) throw new ArgumentNullException(nameof(targetMethod));
        var arguments = args ?? Array.Empty<object?>();
        var aspectContext = GenerateAspectContext(targetMethod, arguments!);

        AspectLogs.AspectContextGenerated(Logger);

        var previousActivity = Activity.Current;
        var activity =
            LibraryActivitySources.ActivitySource.StartActivity(
                $"{targetMethod.DeclaringType?.Name}.{targetMethod.Name}");
        if (activity != null)
        {
            activity.SetTag("code.namespace", targetMethod.DeclaringType?.Namespace);
            activity.SetTag("code.function", targetMethod.Name);
            activity.SetTag("aspect.factory", FactoryType.FullName);
            activity.SetTag("aspect.target_type", ObjectType.FullName);
            activity.SetTag("aspect.interface_type", typeof(T).FullName);
        }

        var sw = Stopwatch.StartNew();
        var isAsync = targetMethod.IsAsync();

        try
        {
            DispatchInvocation(aspectContext, isAsync);

            if (isAsync && aspectContext.ReturnValue is Task taskResult)
            {
                AttachAsyncTelemetry(taskResult, activity, sw);
                // Restore the caller's Activity.Current so our span doesn't leak into
                // the caller's context for any work it does between receiving the Task
                // and awaiting it. The activity itself is stopped/disposed in the
                // continuation registered by AttachAsyncTelemetry.
                Activity.Current = previousActivity;
                return aspectContext.ReturnValue;
            }

            CompleteSuccess(activity, sw);
            return aspectContext.ReturnValue;
        }
        catch (Exception ex)
        {
            CompleteError(activity, sw, ex);
            throw;
        }
    }

    /// <summary>
    /// Routes the invocation to either the intercepted or pass-through path based on
    /// <see cref="ShouldIntercept" />. Extracted from <see cref="Invoke(System.Reflection.MethodInfo, object[])" />
    /// to keep its cognitive complexity below the Sonar S3776 threshold.
    /// </summary>
    private void DispatchInvocation(AspectContext aspectContext, bool isAsync)
    {
        if (ShouldIntercept(aspectContext))
        {
            DispatchIntercepted(aspectContext, isAsync);
        }
        else
        {
            AspectLogs.InvokingWithoutInterception(Logger, aspectContext.InvocationString ?? "Unknown");
            InvokeWithoutInterception(aspectContext);
        }
    }

    /// <summary>
    /// Runs the aspect's <see cref="PreInvoke" />, dispatches to the target (or honors a
    /// short-circuit), and wires <see cref="PostInvoke" /> appropriately for sync vs async paths.
    /// </summary>
    private void DispatchIntercepted(AspectContext aspectContext, bool isAsync)
    {
        PreInvoke(aspectContext);

        if (aspectContext.InvokeMethod)
        {
            AspectLogs.InvokingWithInterception(Logger, aspectContext.InvocationString ?? "Unknown");
            Invoke(aspectContext);
            // For async methods, PostInvoke is wired into the task continuation by
            // ProcessAction / CallProcessFunction (via BaseAspectAsyncProcessor). For sync
            // methods, PostInvoke must be invoked here.
            if (!isAsync) PostInvoke(aspectContext);
            return;
        }

        if (isAsync && aspectContext.ReturnValue is Task shortCircuitTask)
        {
            // Async short-circuit: the aspect supplied a Task return value without invoking
            // the target. Wire PostInvoke through the same async machinery as the normal
            // intercepted path so PostInvoke observes the unwrapped TResult (not the
            // Task<TResult> wrapper) and so cleanup reliably runs in the task's continuation.
            HandleAsyncShortCircuit(aspectContext, shortCircuitTask);
            return;
        }

        // Sync short-circuit, or async short-circuit with no Task supplied. Run PostInvoke
        // immediately so aspects can reliably perform cleanup.
        PostInvoke(aspectContext);
    }

    /// <summary>
    /// Handles an async short-circuit: routes <see cref="Task{TResult}" /> results through the
    /// generic async processor so <see cref="PostInvoke" /> observes the unwrapped result, and
    /// wraps non-generic <see cref="Task" /> results so <see cref="PostInvoke" /> is awaited as
    /// part of the returned task's completion.
    /// </summary>
    private void HandleAsyncShortCircuit(AspectContext aspectContext, Task shortCircuitTask)
    {
        var taskType = shortCircuitTask.GetType();
        if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = taskType.GetGenericArguments()[0];
            var mi = BaseAspectAsyncProcessor.ProcessFunctionMethodInfo.MakeGenericMethod(resultType);
            aspectContext.ReturnValue = mi.Invoke(
                null,
                BindingFlags.DoNotWrapExceptions,
                binder: null,
                parameters: [shortCircuitTask, aspectContext, (Action<AspectContext>)PostInvoke],
                culture: null);
            return;
        }

        // Non-generic Task — wrap so the caller's await on the returned task observes PostInvoke
        // having completed before resuming. A fire-and-forget ContinueWith would let the caller's
        // continuation race PostInvoke, which is the bug this branch previously had.
        aspectContext.ReturnValue = BaseAspectAsyncProcessor.ProcessActionAsync(
            shortCircuitTask, aspectContext, PostInvoke);
    }

    /// <summary>
    /// Pass-through dispatch for methods filtered out by <see cref="ShouldIntercept" />. Invokes
    /// the target method and surfaces its return value (sync result or <see cref="Task" />) without
    /// running <see cref="PreInvoke" /> or <see cref="PostInvoke" />.
    /// </summary>
    private void InvokeWithoutInterception(AspectContext aspectContext)
    {
        aspectContext.ReturnValue = aspectContext.TargetMethod.Invoke(
            Instance,
            BindingFlags.DoNotWrapExceptions,
            binder: null,
            parameters: aspectContext.ParameterValues,
            culture: null);
    }

    /// <summary>
    /// Records success metrics and disposes the activity. Used for both synchronous completion
    /// and as the success branch of <see cref="AttachAsyncTelemetry" />.
    /// </summary>
    private void CompleteSuccess(Activity? activity, Stopwatch sw)
    {
        sw.Stop();
        LibraryMeters.InvocationsCounter.Add(1, new TagList { { Aspect, FactoryType.Name }, { "status", "success" } });
        LibraryMeters.DurationHistogram.Record(sw.Elapsed.TotalMilliseconds,
            new TagList { { Aspect, FactoryType.Name } });
        activity?.Dispose();
    }

    /// <summary>
    /// Records error metrics, sets the activity status to <see cref="ActivityStatusCode.Error" />,
    /// adds an <c>exception</c> activity event, and disposes the activity.
    /// </summary>
    private void CompleteError(Activity? activity, Stopwatch sw, Exception ex)
    {
        sw.Stop();
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message },
                { "exception.stacktrace", ex.StackTrace }
            }));
        }

        LibraryMeters.InvocationsCounter.Add(1, new TagList { { Aspect, FactoryType.Name }, { "status", "error" } });
        LibraryMeters.DurationHistogram.Record(sw.Elapsed.TotalMilliseconds,
            new TagList { { Aspect, FactoryType.Name } });
        activity?.Dispose();
    }

    /// <summary>
    /// Defers telemetry completion until <paramref name="taskResult" /> settles, so that
    /// <c>aspect.invocations</c>, <c>aspect.invocation_duration</c>, and the activity status
    /// reflect the actual outcome and duration of the asynchronous operation.
    /// </summary>
    /// <remarks>
    /// The continuation is fire-and-forget and runs synchronously when the task settles to
    /// keep the recorded duration as close to actual completion as possible. The original
    /// task is returned to the caller unchanged.
    /// </remarks>
    private void AttachAsyncTelemetry(Task taskResult, Activity? activity, Stopwatch sw)
    {
        taskResult.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                var ex = t.Exception?.InnerException ?? (Exception?)t.Exception ?? new Exception("Task faulted");
                CompleteError(activity, sw, ex);
            }
            else if (t.IsCanceled)
            {
                CompleteError(activity, sw, new TaskCanceledException(t));
            }
            else
            {
                CompleteSuccess(activity, sw);
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// Runs after a configured aspect has invoked the target method.
    /// </summary>
    /// <param name="aspectContext">
    /// The invocation context containing the target method, argument values, and return value.
    /// </param>
    public virtual void PostInvoke(AspectContext aspectContext)
    {
    }

    /// <summary>
    /// Runs before a configured aspect invokes the target method.
    /// </summary>
    /// <param name="aspectContext">
    /// The invocation context that can be inspected or modified before dispatch.
    /// </param>
    public virtual void PreInvoke(AspectContext aspectContext)
    {
    }

    /// <summary>
    /// Determines whether the current aspect should intercept the supplied invocation.
    /// </summary>
    /// <param name="aspectContext">The invocation context for the target method.</param>
    /// <returns>
    /// <see langword="true" /> when the aspect's pre/post hooks should run; otherwise
    /// <see langword="false" />.
    /// </returns>
    public virtual bool ShouldIntercept(AspectContext aspectContext)
    {
        return AspectConfigurationProvider.ShouldIntercept(FactoryType, aspectContext.TargetMethod.DeclaringType!,
            ObjectType, aspectContext.TargetMethod);
    }

    /// <summary>
    /// Invokes a target method that returns <see cref="Task{TResult}" /> and stores the wrapped task
    /// on the invocation context.
    /// </summary>
    /// <param name="aspectContext">
    /// The invocation context for the asynchronous function call.
    /// </param>
    private void CallProcessFunction(AspectContext aspectContext)
    {
        var resultType = aspectContext.TargetMethod.ReturnType.GetGenericArguments()[0];
        var mi = BaseAspectAsyncProcessor.ProcessFunctionMethodInfo.MakeGenericMethod(resultType);
        var task = aspectContext.TargetMethod.Invoke(
            Instance,
            BindingFlags.DoNotWrapExceptions,
            binder: null,
            parameters: aspectContext.ParameterValues,
            culture: null);
        aspectContext.ReturnValue = mi.Invoke(
            null,
            BindingFlags.DoNotWrapExceptions,
            binder: null,
            parameters: [task, aspectContext, (Action<AspectContext>)PostInvoke],
            culture: null);
    }

    /// <summary>
    /// Invokes the target method based on its return type (sync, Task, or Task{T}).
    /// </summary>
    /// <param name="aspectContext">The context for the current invocation.</param>
    private void Invoke(AspectContext aspectContext)
    {
        switch (aspectContext.MethodType)
        {
            case MethodTypeOptions.AsyncAction:
                aspectContext.ReturnValue = ProcessAction(aspectContext);
                break;
            case MethodTypeOptions.AsyncFunction:
                CallProcessFunction(aspectContext);
                break;
            default:
                Process(aspectContext);
                break;
        }
    }

    /// <summary>
    /// Invokes a synchronous target method and stores its return value on the invocation context.
    /// </summary>
    /// <param name="aspectContext">
    /// The invocation context for the synchronous call.
    /// </param>
    private void Process(AspectContext aspectContext)
    {
        aspectContext.ReturnValue = aspectContext.TargetMethod.Invoke(
            Instance,
            BindingFlags.DoNotWrapExceptions,
            binder: null,
            parameters: aspectContext.ParameterValues,
            culture: null);
    }

    /// <summary>
    /// Invokes a target method that returns <see cref="Task" /> and runs post-invocation logic when
    /// the task completes.
    /// </summary>
    /// <param name="aspectContext">
    /// The invocation context for the asynchronous action call.
    /// </param>
    /// <returns>
    /// A task that represents the intercepted asynchronous action.
    /// </returns>
    private async Task ProcessAction(AspectContext aspectContext)
    {
        var task = (Task)aspectContext.TargetMethod.Invoke(
            Instance,
            BindingFlags.DoNotWrapExceptions,
            binder: null,
            parameters: aspectContext.ParameterValues,
            culture: null)!;
        try
        {
            await task.ConfigureAwait(false);
        }
        finally
        {
            PostInvoke(aspectContext);
        }
    }
}
