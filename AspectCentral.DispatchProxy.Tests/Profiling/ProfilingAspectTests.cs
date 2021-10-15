// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfilingAspectTests.cs" company="James Consulting LLC">
//   
// </copyright>
// // <summary>
//   The profiling aspect tests.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy.Logging;
using AspectCentral.DispatchProxy.Profiling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests.Profiling
{
    /// <summary>
    ///     The profiling aspect tests.
    /// </summary>
    public class ProfilingAspectTests
    {
        /// <summary>
        ///     Gets or sets the aspect configuration provider
        /// </summary>
        private readonly IAspectConfigurationProvider aspectConfigurationProvider;

        /// <summary>
        ///     The instance.
        /// </summary>
        private readonly ITestInterface instance;

        /// <summary>
        ///     The logger.
        /// </summary>
        private readonly Mock<ILogger> logger;

        /// <summary>
        ///     The logger factory.
        /// </summary>
        private readonly Mock<ILoggerFactory> loggerFactory;

        /// <summary>
        ///     The test logging async.
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task ProfilingAsync()
        {
            await instance.TestAsync(1, "2", null);
            logger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true), It.IsAny<Exception>(), It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Exactly(2));
        }

        /// <summary>
        ///     The test logging async with result.
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task ProfilingAsyncWithResult()
        {
            await instance.GetClassByIdAsync(1);
            logger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true), It.IsAny<Exception>(), It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Exactly(2));
        }

        /// <summary>
        ///     The my test method.
        /// </summary>
        [Fact]
        public void ProfilingSyncMethod()
        {
            instance.Test(1, "2", new MyUnitTestClass(1, "2"));
            logger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true), It.IsAny<Exception>(), It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Exactly(2));
        }

        /// <summary>
        ///     The test initialize.
        /// </summary>
        public ProfilingAspectTests()
        {
            loggerFactory = new Mock<ILoggerFactory>();
            logger = new Mock<ILogger>();
            aspectConfigurationProvider = new InMemoryAspectConfigurationProvider();
            var aspectConfiguration = new AspectConfiguration(new ServiceDescriptor(AspectRegistrationTests.IInterfaceType, AspectRegistrationTests.MyTestInterfaceType, ServiceLifetime.Transient));
            aspectConfiguration.AddEntry(LoggingAspectFactory.LoggingAspectFactoryType, methodsToIntercept: AspectRegistrationTests.IInterfaceType.GetMethods());
            aspectConfiguration.AddEntry(ProfilingAspectFactory.ProfilingAspectFactoryType, methodsToIntercept: AspectRegistrationTests.IInterfaceType.GetMethods());
            aspectConfigurationProvider.AddEntry(aspectConfiguration);
            loggerFactory.Setup(x => x.CreateLogger(typeof(MyTestInterface).FullName)).Returns(logger.Object);
            instance = ProfilingAspect<ITestInterface>.Create(
                new MyTestInterface(),
                typeof(MyTestInterface),
                loggerFactory.Object,
                aspectConfigurationProvider,
                ProfilingAspectFactory.ProfilingAspectFactoryType);
        }

        [Fact]
        public void CreateNullInstanceThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ProfilingAspect<ITestInterface>.Create(
                null,
                typeof(MyTestInterface),
                loggerFactory.Object,
                aspectConfigurationProvider,
                ProfilingAspectFactory.ProfilingAspectFactoryType));
        }
        
        [Fact]
        public void CreateNullTypeThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ProfilingAspect<ITestInterface>.Create(
                new MyTestInterface(), 
                null,
                loggerFactory.Object,
                aspectConfigurationProvider,
                ProfilingAspectFactory.ProfilingAspectFactoryType));
        }
        
        [Fact]
        public void CreateNullLoggerThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ProfilingAspect<ITestInterface>.Create(
                new MyTestInterface(), 
                typeof(MyTestInterface),
                null,
                aspectConfigurationProvider,
                ProfilingAspectFactory.ProfilingAspectFactoryType));
        }
        
        [Fact]
        public void CreateNullAspectConfigurationProviderThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ProfilingAspect<ITestInterface>.Create(
                new MyTestInterface(), 
                typeof(MyTestInterface),
                loggerFactory.Object,
                null,
                ProfilingAspectFactory.ProfilingAspectFactoryType));
        }
        
        [Fact]
        public void CreateNullFactoryTypeThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ProfilingAspect<ITestInterface>.Create(
                new MyTestInterface(), 
                typeof(MyTestInterface),
                loggerFactory.Object,
                aspectConfigurationProvider,
                null));
        }
        
    }
}