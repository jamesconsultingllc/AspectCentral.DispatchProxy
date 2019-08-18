// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfilingAspect.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The profiling aspect.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Profiling
{
    /// <summary>
    /// The logging aspect.
    /// </summary>
    /// <typeparam name="T">
    /// </typeparam>
    public class ProfilingAspect<T> : BaseAspect<T>
    {
        /// <summary>
        ///     Gets the ProfilingAspectType
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static readonly Type Type = typeof(ProfilingAspect<>);

        /// <summary>
        ///     The stopwatch.
        /// </summary>
        private readonly Stopwatch stopWatch = new Stopwatch();

        /// <summary>
        /// The create.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="type">
        /// </param>
        /// <param name="loggerFactory">
        /// The logger.
        /// </param>
        /// <param name="aspectConfigurationProvider">
        /// </param>
        /// <param name="profilingAspectFactoryType">
        /// </param>
        /// <returns>
        /// The <see cref="T"/>.
        /// </returns>
        public static T Create(T instance, Type type, ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider, Type profilingAspectFactoryType)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            if (aspectConfigurationProvider == null)
                throw new ArgumentNullException(nameof(aspectConfigurationProvider));
            if (profilingAspectFactoryType == null) throw new ArgumentNullException(nameof(profilingAspectFactoryType));
            
            object proxy = Create<T, ProfilingAspect<T>>();
            ((ProfilingAspect<T>)proxy).Instance = instance;
            ((ProfilingAspect<T>)proxy).ObjectType = type;
            ((ProfilingAspect<T>)proxy).Logger = loggerFactory.CreateLogger(type.FullName);
            ((ProfilingAspect<T>)proxy).AspectConfigurationProvider = aspectConfigurationProvider;
            ((ProfilingAspect<T>)proxy).FactoryType = profilingAspectFactoryType;
            return (T)proxy;
        }

        /// <summary>
        /// The post invoke.
        /// </summary>
        /// <param name="aspectContext">
        /// The invocation context.
        /// </param>
        protected override void PostInvoke(AspectContext aspectContext)
        {
            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            Logger.LogInformation($"Runtime {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}");
        }

        /// <summary>
        /// The pre invoke.
        /// </summary>
        /// <param name="aspectContext">
        /// The invocation context.
        /// </param>
        protected override void PreInvoke(AspectContext aspectContext)
        {
            Logger.LogInformation("Starting Stopwatch");
            stopWatch.Start();
        }
    }
}