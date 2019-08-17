// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfilingAspectRegistrationBuilderExtensionsTests.cs" company="James Consulting LLC">
//   
// </copyright>
//  <summary>
//   The profiling aspect registration builder extensions tests.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using AspectCentral.Abstractions;
using AspectCentral.DispatchProxy.Profiling;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests.Profiling
{
    /// <summary>
    ///     The profiling aspect registration builder extensions tests.
    /// </summary>
    public class ProfilingAspectRegistrationBuilderExtensionsTests
    {
        /// <summary>
        ///     The add profiling aspect null builder throws argument null exception.
        /// </summary>
        [Fact]
        public void AddProfilingAspectNullBuilderThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => default(IAspectRegistrationBuilder).AddProfilingAspect());
        }

        /// <summary>
        ///     The add profiling aspect registers all methods when no methods are given.
        /// </summary>
        [Fact]
        public void AddProfilingAspectRegistersAllMethodsWhenNoMethodsAreGiven()
        {
            var builder = new ServiceCollection().AddAspectSupport().AddTransient<ITestInterface, MyTestInterface>().AddProfilingAspect();

            var aspects = builder.AspectConfigurationProvider.ConfigurationEntries.Last().GetAspects().ToArray();
            Assert.Equal(ProfilingAspectFactory.ProfilingAspectFactoryType, aspects[0].AspectFactoryType);
        }
    }
}