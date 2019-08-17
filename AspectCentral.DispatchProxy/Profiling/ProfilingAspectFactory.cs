// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfilingAspectFactory.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The profiling aspect factory.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Profiling
{
    /// <summary>
    ///     The logging aspect factory.
    /// </summary>
    public class ProfilingAspectFactory : BaseAspectFactory
    {
        /// <summary>
        ///     The profiling aspect factory type.
        /// </summary>
        public static readonly Type ProfilingAspectFactoryType = typeof(ProfilingAspectFactory);

        /// <inheritdoc />
        public ProfilingAspectFactory(ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider) : base(loggerFactory, aspectConfigurationProvider)
        {
        }

        /// <inheritdoc />
        public override T Create<T>(T instance, Type implementationType)
        {
            return ProfilingAspect<T>.Create(instance, implementationType, LoggerFactory, AspectConfigurationProvider, ProfilingAspectFactoryType);
        }
    }
}