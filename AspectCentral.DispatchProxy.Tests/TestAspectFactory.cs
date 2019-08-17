//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="TestAspectFactory.cs" company="James Consulting LLC">
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

namespace AspectCentral.DispatchProxy.Tests
{
    public class TestAspectFactory : BaseAspectFactory
    {
        public static readonly Type Type = typeof(TestAspectFactory);

        public override T Create<T>(T instance, Type implementationType)
        {
            return instance;
        }

        /// <inheritdoc />
        public TestAspectFactory(ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider) : base(loggerFactory, aspectConfigurationProvider)
        {
        }
    }

    public class TestAspectFactory2 : BaseAspectFactory
    {
        public static readonly Type Type = typeof(TestAspectFactory2);

        public override T Create<T>(T instance, Type implementationType)
        {
            return instance;
        }

        /// <inheritdoc />
        public TestAspectFactory2(ILoggerFactory loggerFactory, IAspectConfigurationProvider aspectConfigurationProvider) : base(loggerFactory, aspectConfigurationProvider)
        {
        }
    }
}