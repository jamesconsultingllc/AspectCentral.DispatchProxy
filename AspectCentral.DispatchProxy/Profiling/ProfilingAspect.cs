// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfilingAspect.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The profiling aspect.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy.Logging;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Profiling;

/// <summary>
///     AOP aspect that measures end-to-end execution time of every intercepted call (including the
///     awaited completion of <see cref="System.Threading.Tasks.Task"/> and
///     <see cref="System.Threading.Tasks.Task{TResult}"/> returns) and emits the elapsed time at
///     <see cref="Microsoft.Extensions.Logging.LogLevel.Information"/> via the source-generated
///     <c>AspectLogs</c> methods (event IDs 3001/3002).
/// </summary>
/// <typeparam name="T">The interface being proxied. Must be a reference type.</typeparam>
public class ProfilingAspect<T> : BaseAspect<T> where T : class?
{
    /// <summary>
    ///     Open generic <see cref="System.Type"/> token (<c>ProfilingAspect&lt;&gt;</c>) used by the
    ///     <see cref="ProfilingAspectFactory"/> to construct the closed generic proxy.
    /// </summary>
    // ReSharper disable once StaticMemberInGenericType
    public static readonly Type Type = typeof(ProfilingAspect<>);

    /// <summary>
    ///     Per-logical-call <see cref="Stopwatch"/>. Stored as <see cref="AsyncLocal{T}"/> so concurrent
    ///     invocations of the same proxy instance — common when the aspect wraps a singleton or scoped
    ///     service — each get an independent start time. The Stopwatch flows across <c>await</c>
    ///     boundaries so <see cref="PostInvoke"/> (which may run on a continuation thread) reads the
    ///     same instance that <see cref="PreInvoke"/> started.
    /// </summary>
    private readonly AsyncLocal<Stopwatch?> stopWatch = new();

    /// <summary>
    ///     Creates a <see cref="ProfilingAspect{T}"/> proxy that wraps <paramref name="instance"/>.
    /// </summary>
    /// <param name="instance">The inner service instance to wrap. Must not be <see langword="null"/>.</param>
    /// <param name="type">The concrete implementation <see cref="System.Type"/> behind <paramref name="instance"/>; used for the logger category and method-info lookup.</param>
    /// <param name="loggerFactory">The factory used to create the logger; the category is set to <c>type.FullName</c>.</param>
    /// <param name="aspectConfigurationProvider">Provider consulted on each call to decide if profiling should fire.</param>
    /// <param name="profilingAspectFactoryType">The <see cref="ProfilingAspectFactory"/> type that owns this proxy. Recorded on every <see cref="System.Diagnostics.Activity"/> as <c>aspect.factory</c>.</param>
    /// <returns>A proxy implementing <typeparamref name="T"/> that times every intercepted call.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null"/>.</exception>
    public static T Create(T instance, Type type, ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider, Type profilingAspectFactoryType)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
        if (aspectConfigurationProvider == null) throw new ArgumentNullException(nameof(aspectConfigurationProvider));
        if (profilingAspectFactoryType == null) throw new ArgumentNullException(nameof(profilingAspectFactoryType));
        
        object proxy = Create<T, ProfilingAspect<T>>()!;
        var profilingAspect = (ProfilingAspect<T>)proxy;
        profilingAspect.Instance = instance!;
        profilingAspect.ObjectType = type;
        profilingAspect.Logger = loggerFactory.CreateLogger(type.FullName ?? type.Name);
        profilingAspect.AspectConfigurationProvider = aspectConfigurationProvider;
        profilingAspect.FactoryType = profilingAspectFactoryType;
        return (T)proxy;
    }

    /// <summary>
    /// The post invoke.
    /// </summary>
    /// <param name="aspectContext">
    /// The invocation context.
    /// </param>
    public override void PostInvoke(AspectContext aspectContext)
    {
        var sw = stopWatch.Value;
        if (sw == null) return;
        sw.Stop();
        var ts = sw.Elapsed;
        AspectLogs.ProfilingAspectEnd(Logger, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
    }

    /// <summary>
    /// The pre invoke.
    /// </summary>
    /// <param name="aspectContext">
    /// The invocation context.
    /// </param>
    public override void PreInvoke(AspectContext aspectContext)
    {
        AspectLogs.ProfilingAspectStart(Logger);
        stopWatch.Value = Stopwatch.StartNew();
    }
}