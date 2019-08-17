//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="BaseAspectFactory.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy
{
    /// <summary>
    ///     The base aspect factory.
    /// </summary>
    public abstract class BaseAspectFactory : IAspectFactory
    {
        /// <summary>
        ///     The aspect configuration provider
        /// </summary>
        protected readonly IAspectConfigurationProvider AspectConfigurationProvider;

        /// <summary>
        ///     The logger factory.
        /// </summary>
        protected readonly ILoggerFactory LoggerFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseAspectFactory" /> class.
        /// </summary>
        /// <param name="loggerFactory">
        ///     The logger factory.
        /// </param>
        /// <param name="aspectConfigurationProvider">
        /// </param>
        protected BaseAspectFactory(ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider)
        {
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            AspectConfigurationProvider = aspectConfigurationProvider ?? throw new ArgumentNullException(nameof(aspectConfigurationProvider));
        }

        /// <inheritdoc />
        public abstract T Create<T>(T instance, Type implementationType);
    }
}