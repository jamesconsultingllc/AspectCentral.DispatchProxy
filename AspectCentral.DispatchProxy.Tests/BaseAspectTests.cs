//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="BaseAspectTests.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests
{
    /// <summary>
    ///     The generic base aspect tests.
    /// </summary>
    public class BaseAspectTests
    {
        /// <summary>
        ///     The test initialize.
        /// </summary>
        public BaseAspectTests()
        {
            var loggerFactory = new Mock<ILoggerFactory>();
            logger = new Mock<ILogger>();
            logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            loggerFactory.Setup(x => x.CreateLogger(typeof(MyTestInterface).FullName!)).Returns(logger.Object);
            var aspectConfigurationProviderMock = new Mock<IAspectConfigurationProvider>();
            aspectConfigurationProviderMock
                .Setup(x => x.ShouldIntercept(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<System.Reflection.MethodInfo>()))
                .Returns(true);
            var aspectConfigurationProvider = aspectConfigurationProviderMock.Object;
            var aspectConfiguration = new AspectConfiguration(new ServiceDescriptor(ITestInterfaceType, MyTestInterface.Type, ServiceLifetime.Transient));
            aspectConfiguration.AddEntry(TestAspectFactory.Type, methodsToIntercept:ITestInterfaceType.GetMethods());
            aspectConfiguration.AddEntry(TestAspectFactory2.Type, methodsToIntercept:ITestInterfaceType.GetMethods());
            aspectConfigurationProvider.AddEntry(aspectConfiguration);
            instance = BaseAspectTestClass<ITestInterface>.Create(new MyTestInterface(), typeof(MyTestInterface), loggerFactory.Object, aspectConfigurationProvider);
        }

        // ReSharper disable once InconsistentNaming
        private static readonly Type ITestInterfaceType = typeof(ITestInterface);

        /// <summary>
        ///     The instance.
        /// </summary>
        private readonly ITestInterface instance;

        /// <summary>
        ///     The logger.
        /// </summary>
        private readonly Mock<ILogger> logger;

        /// <summary>
        ///     The test creating task result when method not invoked.
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task TestCreatingTaskResultWhenMethodNotInvoked()
        {
            // PreInvoke synthesizes a Task result and sets InvokeMethod=false, so the real method is never called.
            // Async + InvokeMethod=false: PreInvoke runs (1 log), inner Invoke is skipped, and PostInvoke does NOT run
            // (outer block gates PostInvoke on !isAsync, and the async wrappers never run because InvokeMethod=false).
            var result = await instance.GetClassByIdAsync(12);
            logger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true), It.IsAny<Exception?>(), It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
            Assert.Equal(new MyUnitTestClass(12, "testing 123"), result);
        }
    }
}