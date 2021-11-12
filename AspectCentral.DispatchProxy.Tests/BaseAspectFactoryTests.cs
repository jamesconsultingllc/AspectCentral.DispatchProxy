//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="BaseAspectFactoryTests.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests
{
    public class BaseAspectFactoryTests
    {
        [Fact]
        public void ConstructorThrowsArgumentNullExceptionWhenLoggerFactoryIsNull()
        {
            Assert.Throws<ArgumentNullException>("loggerFactory", () => new TestAspectFactory(null, null));
        }

        [Fact]
        public void ConstructorThrowsArgumentNullExceptionWhenAspectConfigurationProviderIsNull()
        {
            Assert.Throws<ArgumentNullException>("aspectConfigurationProvider", () => new TestAspectFactory(new NullLoggerFactory(), null));
        }
    }
}