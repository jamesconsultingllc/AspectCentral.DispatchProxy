// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingAspectRegistrationBuilderExtensionsTests.cs" company="James Consulting LLC">
//   
// </copyright>
//  <summary>
//   The logging aspect registration builder extensions tests.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using AspectCentral.Abstractions;
using AspectCentral.DispatchProxy.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests.Logging
{
    /// <summary>
    /// The logging aspect registration builder extensions tests.
    /// </summary>
    public class LoggingAspectRegistrationBuilderExtensionsTests
    {
        /// <summary>
        /// The add logging aspect null builder throws argument null exception.
        /// </summary>
        [Fact]
        public void AddLoggingAspectNullBuilderThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => default(IAspectRegistrationBuilder).AddLoggingAspect());
        }

        /// <summary>
        /// The add logging aspect registers all methods when no methods are given.
        /// </summary>
        [Fact]
        public void AddLoggingAspectRegistersAllMethodsWhenNoMethodsAreGiven()
        {
            var builder = new ServiceCollection().AddAspectSupport().AddTransient<ITestInterface, MyTestInterface>().AddLoggingAspect();

            var aspects = builder.AspectConfigurationProvider.ConfigurationEntries[0].GetAspects().ToArray();
            Assert.Equal(typeof(MyTestInterface), builder.AspectConfigurationProvider.ConfigurationEntries[0].ServiceDescriptor.ImplementationType);
            Assert.Single(aspects);
            Assert.Equal(LoggingAspectFactory.LoggingAspectFactoryType, aspects[0].AspectFactoryType);
        }
    }
}