// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingAspect.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The logging aspect.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using JamesConsulting.Reflection;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Logging
{
    /// <summary>
    /// The logging aspect.
    /// </summary>
    /// <typeparam name="T">
    /// </typeparam>
    public class LoggingAspect<T> : BaseAspect<T>
    {
        /// <summary>
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static readonly Type Type = typeof(LoggingAspect<>);

        /// <summary>
        ///     The logger.
        /// </summary>
        private ILogger logger;

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
        /// <param name="loggingAspectFactoryType">
        /// </param>
        /// <returns>
        /// The <see cref="T"/>.
        /// </returns>
        public static T Create(T instance, Type type, ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider, Type loggingAspectFactoryType)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            if (aspectConfigurationProvider == null)
                throw new ArgumentNullException(nameof(aspectConfigurationProvider));
            if (loggingAspectFactoryType == null) throw new ArgumentNullException(nameof(loggingAspectFactoryType));
            
            object proxy = Create<T, LoggingAspect<T>>();
            ((LoggingAspect<T>)proxy).Instance = instance;
            ((LoggingAspect<T>)proxy).ObjectType = type;
            ((LoggingAspect<T>)proxy).logger = loggerFactory.CreateLogger(type.FullName);
            ((LoggingAspect<T>)proxy).AspectConfigurationProvider = aspectConfigurationProvider;
            ((LoggingAspect<T>)proxy).FactoryType = loggingAspectFactoryType;
            return (T)proxy;
        }

        /// <summary>
        /// The post invoke.
        /// </summary>
        /// <param name="aspectContext">
        /// The aspect context.
        /// </param>
        protected override void PostInvoke(AspectContext aspectContext)
        {
            if (aspectContext.TargetMethod.HasReturnValue()) logger.LogInformation($"Return value : {aspectContext.ReturnValue}");

            logger.LogInformation($"{aspectContext.InvocationString} End");
        }

        /// <summary>
        /// The pre invoke.
        /// </summary>
        /// <param name="aspectContext">
        /// The aspect context.
        /// </param>
        protected override void PreInvoke(AspectContext aspectContext)
        {
            logger.LogInformation($"{aspectContext.InvocationString} Start");
        }
    }
}