// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingAspect.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The logging aspect.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using JamesConsulting.Reflection;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Logging;

/// <summary>
///     AOP aspect that emits structured log entries before, after, and (when applicable) for the
///     return value of every intercepted call. Uses the source-generated <c>AspectLogs</c> methods
///     (event IDs 2001/2002/2003) so the hot path does no string formatting unless a logging
///     provider is enabled at <see cref="Microsoft.Extensions.Logging.LogLevel.Information"/>.
/// </summary>
/// <typeparam name="T">The interface being proxied. Must be a reference type.</typeparam>
public class LoggingAspect<T> : BaseAspect<T> where T : class?
{
    /// <summary>
    ///     Open generic <see cref="System.Type"/> token (<c>LoggingAspect&lt;&gt;</c>) used by the
    ///     <see cref="LoggingAspectFactory"/> to construct the closed generic proxy.
    /// </summary>
    // ReSharper disable once StaticMemberInGenericType
    public static readonly Type Type = typeof(LoggingAspect<>);

    /// <summary>
    ///     Creates a <see cref="LoggingAspect{T}"/> proxy that wraps <paramref name="instance"/>.
    /// </summary>
    /// <param name="instance">The inner service instance to wrap. Must not be <see langword="null"/>.</param>
    /// <param name="type">The concrete implementation <see cref="System.Type"/> behind <paramref name="instance"/>; used for the logger category and method-info lookup.</param>
    /// <param name="loggerFactory">The factory used to create the logger; the category is set to <c>type.FullName</c> so consumers can filter per implementation.</param>
    /// <param name="aspectConfigurationProvider">Provider consulted on each call to decide if logging should fire.</param>
    /// <param name="loggingAspectFactoryType">The <see cref="LoggingAspectFactory"/> type that owns this proxy. Recorded on every <see cref="System.Diagnostics.Activity"/> as <c>aspect.factory</c>.</param>
    /// <returns>A proxy implementing <typeparamref name="T"/> that logs every intercepted call.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null"/>.</exception>
    public static T Create(T instance, Type type, ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider, Type loggingAspectFactoryType)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
        if (aspectConfigurationProvider == null) throw new ArgumentNullException(nameof(aspectConfigurationProvider));
        if (loggingAspectFactoryType == null) throw new ArgumentNullException(nameof(loggingAspectFactoryType));
        
        object proxy = Create<T, LoggingAspect<T>>()!;
        var loggingAspect = (LoggingAspect<T>)proxy;
        loggingAspect.Instance = instance!;
        loggingAspect.ObjectType = type;
        loggingAspect.Logger = loggerFactory.CreateLogger(type.FullName ?? type.Name);
        loggingAspect.AspectConfigurationProvider = aspectConfigurationProvider;
        loggingAspect.FactoryType = loggingAspectFactoryType;
        return (T)proxy;
    }

    /// <summary>
    /// The post invoke.
    /// </summary>
    /// <param name="aspectContext">
    /// The aspect context.
    /// </param>
    public override void PostInvoke(AspectContext aspectContext)
    {
        if (aspectContext.TargetMethod.HasReturnValue())
        {
            AspectLogs.LoggingAspectReturnValue(Logger, aspectContext.ReturnValue);
        }

        AspectLogs.LoggingAspectEnd(Logger, aspectContext.InvocationString ?? "Unknown");
    }

    /// <summary>
    /// The pre invoke.
    /// </summary>
    /// <param name="aspectContext">
    /// The aspect context.
    /// </param>
    public override void PreInvoke(AspectContext aspectContext)
    {
        AspectLogs.LoggingAspectStart(Logger, aspectContext.InvocationString ?? "Unknown");
    }
}