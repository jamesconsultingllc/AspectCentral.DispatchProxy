//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="BaseAspect.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
//    Provides a base implementation for AOP aspects using <see cref="System.Reflection.DispatchProxy"/>.
//    This class handles the complexity of method interception, including support for synchronous
//    and asynchronous (Task, Task&lt;T&gt;) methods, and provides built-in observability.
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy.Logging;
using AspectCentral.DispatchProxy.Telemetry;
using JamesConsulting.Reflection;
using Microsoft.Extensions.Logging;
using MethodTypeOptions = AspectCentral.Abstractions.MethodTypeOptions;

namespace AspectCentral.DispatchProxy;

/// <summary>
///     Base class for implementing AOP aspects using <see cref="System.Reflection.DispatchProxy"/>.
///     Provides hooks for pre- and post-invocation logic, supports sync and async methods,
///     and includes built-in observability via OpenTelemetry.
/// </summary>
/// <typeparam name="T">The interface type being proxied.</typeparam>
public abstract class BaseAspect<T> : System.Reflection.DispatchProxy where T : class?
{
    /// <summary>
    ///     Cached MethodInfo for the generic async result processor.
    /// </summary>
    private static readonly MethodInfo ProcessFunctionMethodInfo =
        typeof(BaseAspect<T>).GetMethod("ProcessFunctionAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    ///     Gets or sets the type of the factory that created this aspect.
    /// </summary>
    protected Type FactoryType { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the provider used to determine if a method should be intercepted.
    /// </summary>
    protected IAspectConfigurationProvider AspectConfigurationProvider { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the underlying service instance being wrapped.
    /// </summary>
    protected T Instance { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the logger used for diagnostic information.
    /// </summary>
    protected ILogger Logger { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the concrete implementation type of the service.
    /// </summary>
    protected Type ObjectType { get; set; } = default!;

    /// <summary>
    ///     Generates the context for the current method invocation.
    /// </summary>
    /// <param name="targetMethod">The method being called on the interface.</param>
    /// <param name="args">The arguments passed to the method.</param>
    /// <returns>A new <see cref="AspectContext"/> containing invocation details.</returns>
    public virtual AspectContext GenerateAspectContext(MethodInfo targetMethod, object[] args)
    {
        return new AspectContext(targetMethod, args)
        {
            InvocationString = GenerateMethodNameWithArguments(targetMethod, args, out var implementationMethod),
            InstanceMethod = implementationMethod
        };
    }

    /// <summary>
    ///     Resolves the concrete implementation method and generates a descriptive invocation string.
    /// </summary>
    /// <param name="targetMethod">The interface method being invoked.</param>
    /// <param name="args">The method arguments.</param>
    /// <param name="implementationMethod">Output parameter containing the resolved <see cref="MethodInfo"/> on the implementation type.</param>
    /// <returns>A string representation of the method call with its arguments.</returns>
    public virtual string GenerateMethodNameWithArguments(MethodInfo targetMethod, object[] args,
        out MethodInfo implementationMethod)
    {
        if (!JamesConsulting.Constants.TypeMethods.ContainsKey(ObjectType))
        {
            AspectLogs.AddedMethodsToCache(Logger, ObjectType.FullName ?? ObjectType.Name);
            JamesConsulting.Constants.TypeMethods[ObjectType] = ObjectType.GetMethods();
        }

        var methodName = targetMethod.IsGenericMethod
            ? targetMethod.GetGenericMethodDefinition().ToString()
            : targetMethod.ToString();
        implementationMethod =
            JamesConsulting.Constants.TypeMethods[ObjectType].Single(x => x.ToString() == methodName);
        return implementationMethod.ToInvocationString(args);
    }

    /// <summary>
    ///     Dispatches the method invocation. Wraps the call in pre/post invocation hooks and telemetry spans.
    /// </summary>
    /// <param name="targetMethod">The method to be invoked.</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>The result of the method invocation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="targetMethod"/> is null.</exception>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) throw new ArgumentNullException(nameof(targetMethod));
        var arguments = args ?? Array.Empty<object?>();
        var aspectContext = GenerateAspectContext(targetMethod, arguments!);

        AspectLogs.AspectContextGenerated(Logger);

        var activity = LibraryActivitySources.ActivitySource.StartActivity($"{targetMethod.DeclaringType?.Name}.{targetMethod.Name}");
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
            if (ShouldIntercept(aspectContext))
            {
                PreInvoke(aspectContext);

                if (aspectContext.InvokeMethod)
                {
                    AspectLogs.InvokingWithInterception(Logger, aspectContext.InvocationString ?? "Unknown");
                    Invoke(aspectContext);
                }

                if (!isAsync)
                {
                    PostInvoke(aspectContext);
                }
            }
            else
            {
                AspectLogs.InvokingWithoutInterception(Logger, aspectContext.InvocationString ?? "Unknown");
                InvokeWithoutInterception(aspectContext);
            }

            if (isAsync && aspectContext.ReturnValue is Task taskResult)
            {
                AttachAsyncTelemetry(taskResult, activity, sw);
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
    ///     Pass-through dispatch for methods filtered out by <see cref="ShouldIntercept"/>. Invokes
    ///     the target method and surfaces its return value (sync result or <see cref="Task"/>) without
    ///     running <see cref="PreInvoke"/> or <see cref="PostInvoke"/>.
    /// </summary>
    private void InvokeWithoutInterception(AspectContext aspectContext)
    {
        aspectContext.ReturnValue = aspectContext.TargetMethod.Invoke(Instance, aspectContext.ParameterValues);
    }

    /// <summary>
    ///     Records success metrics and disposes the activity. Used for both synchronous completion
    ///     and as the success branch of <see cref="AttachAsyncTelemetry"/>.
    /// </summary>
    private void CompleteSuccess(Activity? activity, Stopwatch sw)
    {
        sw.Stop();
        LibraryMeters.InvocationsCounter.Add(1, new TagList { { "aspect", FactoryType.Name }, { "status", "success" } });
        LibraryMeters.DurationHistogram.Record(sw.Elapsed.TotalMilliseconds, new TagList { { "aspect", FactoryType.Name } });
        activity?.Dispose();
    }

    /// <summary>
    ///     Records error metrics, sets the activity status to <see cref="ActivityStatusCode.Error"/>,
    ///     adds an <c>exception</c> activity event, and disposes the activity.
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
        LibraryMeters.InvocationsCounter.Add(1, new TagList { { "aspect", FactoryType.Name }, { "status", "error" } });
        LibraryMeters.DurationHistogram.Record(sw.Elapsed.TotalMilliseconds, new TagList { { "aspect", FactoryType.Name } });
        activity?.Dispose();
    }

    /// <summary>
    ///     Defers telemetry completion until <paramref name="taskResult"/> settles, so that
    ///     <c>aspect.invocations</c>, <c>aspect.invocation_duration</c>, and the activity status
    ///     reflect the actual outcome and duration of the asynchronous operation.
    /// </summary>
    /// <remarks>
    ///     The continuation is fire-and-forget and runs synchronously when the task settles to
    ///     keep the recorded duration as close to actual completion as possible. The original
    ///     task is returned to the caller unchanged.
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
    ///     The post invoke.
    /// </summary>
    /// <param name="aspectContext">
    ///     The aspect Context.
    /// </param>
    public virtual void PostInvoke(AspectContext aspectContext)
    {
    }

    /// <summary>
    ///     The pre invoke.
    /// </summary>
    /// <param name="aspectContext">
    ///     The aspect Context.
    /// </param>
    public virtual void PreInvoke(AspectContext aspectContext)
    {
    }

    /// <summary>
    ///     The should intercept.
    /// </summary>
    /// <returns>
    ///     The <see cref="bool" />.
    /// </returns>
    public virtual bool ShouldIntercept(AspectContext aspectContext)
    {
        return AspectConfigurationProvider.ShouldIntercept(FactoryType, aspectContext.TargetMethod.DeclaringType!,
            ObjectType, aspectContext.TargetMethod);
    }

    /// <summary>
    ///     The call process function.
    /// </summary>
    /// <param name="aspectContext">
    ///     The aspect context.
    /// </param>
    private void CallProcessFunction(AspectContext aspectContext)
    {
        var resultType = aspectContext.TargetMethod.ReturnType.GetGenericArguments()[0];
        var mi = ProcessFunctionMethodInfo.MakeGenericMethod(resultType);
        var task = aspectContext.TargetMethod.Invoke(Instance, aspectContext.ParameterValues);
        aspectContext.ReturnValue = mi.Invoke(this, new[] { task, aspectContext });
    }

    /// <summary>
    ///     Invokes the target method based on its return type (sync, Task, or Task{T}).
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
    ///     The process.
    /// </summary>
    /// <param name="aspectContext">
    ///     The aspect Context.
    /// </param>
    private void Process(AspectContext aspectContext)
    {
        aspectContext.ReturnValue = aspectContext.TargetMethod.Invoke(Instance, aspectContext.ParameterValues);
    }

    /// <summary>
    ///     The process action async.
    /// </summary>
    /// <param name="aspectContext">
    ///     The aspect context.
    /// </param>
    /// <returns>
    ///     The <see cref="Task" />.
    /// </returns>
    private async Task ProcessAction(AspectContext aspectContext)
    {
        var task = (Task)aspectContext.TargetMethod.Invoke(Instance, aspectContext.ParameterValues)!;
        try
        {
            await task;
        }
        finally
        {
            PostInvoke(aspectContext);
        }
    }

    /// <summary>
    ///     The process function async.
    /// </summary>
    /// <param name="task">
    ///     The task.
    /// </param>
    /// <param name="aspectContext">
    ///     The aspect context.
    /// </param>
    /// <typeparam name="TK">
    /// </typeparam>
    /// <returns>
    ///     The <see cref="Task{TK}" />.
    /// </returns>

    // ReSharper disable once UnusedMember.Local
#pragma warning disable S1144 // Unused private types or members should be removed
    private async Task<TK> ProcessFunctionAsync<TK>(Task<TK> task, AspectContext aspectContext)
    {
        try
        {
            var result = await task;
            aspectContext.ReturnValue = result;
            return result;
        }
        finally
        {
            PostInvoke(aspectContext);
        }
    }

#pragma warning restore S1144 // Unused private types or members should be removed
}