// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingAspectFactory.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The logging aspect factory.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Logging
{
    /// <summary>
    ///     The logging aspect factory.
    /// </summary>
    public class LoggingAspectFactory : BaseAspectFactory
    {
        /// <summary>
        /// The logging aspect factory type.
        /// </summary>
        public static readonly Type LoggingAspectFactoryType = typeof(LoggingAspectFactory);

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingAspectFactory"/> class.
        /// </summary>
        /// <param name="loggerFactory">
        /// The logger factory.
        /// </param>
        /// <param name="aspectConfigurationProvider">
        /// </param>
        public LoggingAspectFactory(ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider) : base(loggerFactory, aspectConfigurationProvider)
        {
        }

        /// <inheritdoc />
        public override T Create<T>(T instance, Type implementationType)
        {
            return LoggingAspect<T>.Create(instance, implementationType, LoggerFactory, AspectConfigurationProvider, LoggingAspectFactoryType);
        }
    }
}