//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="BaseAspectTestClass.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using JamesConsulting.Threading;
using Microsoft.Extensions.Logging;

namespace AspectCentral.DispatchProxy.Tests
{
    /// <summary>
    ///     The generic base aspect tests.
    /// </summary>
    /// <typeparam name="T">
    /// </typeparam>
    public class BaseAspectTestClass<T> : BaseAspect<T> where T : class?
    {
        /// <summary>
        ///     The create.
        /// </summary>
        /// <param name="instance">
        ///     The instance.
        /// </param>
        /// <param name="type">
        /// </param>
        /// <param name="loggerFactory">
        ///     The logger.
        /// </param>
        /// <param name="inMemoryAspectConfigurationProvider">
        /// </param>
        /// <returns>
        ///     The <see cref="T" />.
        /// </returns>
        public static T Create(T instance, Type type, ILoggerFactory loggerFactory, IAspectConfigurationProvider inMemoryAspectConfigurationProvider)
        {
            object proxy = Create<T, BaseAspectTestClass<T>>();
            ((BaseAspectTestClass<T>) proxy).Instance = instance;
            ((BaseAspectTestClass<T>) proxy).ObjectType = type;
            ((BaseAspectTestClass<T>) proxy).AspectConfigurationProvider = inMemoryAspectConfigurationProvider;
            ((BaseAspectTestClass<T>) proxy).Logger = loggerFactory.CreateLogger(type.FullName);
            ((BaseAspectTestClass<T>) proxy).FactoryType = TestAspectFactory.Type;
            return (T) proxy;
        }

        /// <summary>
        ///     The post invoke.
        /// </summary>
        /// <param name="aspectContext">
        ///     The aspect context.
        /// </param>
        public override void PostInvoke(AspectContext aspectContext)
        {
            Logger.LogInformation("Should not be invoked");
        }

        /// <summary>
        ///     The pre invoke.
        /// </summary>
        /// <param name="aspectContext">
        ///     The aspect context.
        /// </param>
        public override void PreInvoke(AspectContext aspectContext)
        {
            Logger.LogInformation("Setting result");
            aspectContext.ReturnValue = aspectContext.TargetMethod.CreateTaskResult(new MyUnitTestClass(12, "testing 123"));
            aspectContext.InvokeMethod = false;
        }
    }
}