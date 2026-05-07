//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="BaseAspectTests.cs" company="James Consulting LLC">
//    Copyright (c) 2019 James Consulting LLC. Licensed under the MIT License.
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System.Reflection;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Tests core <see cref="BaseAspect{T}" /> proxy behavior.
/// </summary>
public class BaseAspectTests
{
    // ReSharper disable once InconsistentNaming
    private static readonly Type ITestInterfaceType = typeof(ITestInterface);

    /// <summary>
    /// Proxied test service used by each test.
    /// </summary>
    private readonly ITestInterface _instance;

    /// <summary>
    /// Mock logger used to verify aspect hook execution.
    /// </summary>
    private readonly Mock<ILogger> _logger;

    /// <summary>
    /// Initializes a proxy whose aspect short-circuits asynchronous method invocation.
    /// </summary>
    public BaseAspectTests()
    {
        var loggerFactory = new Mock<ILoggerFactory>();
        _logger = new Mock<ILogger>();
        _logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        loggerFactory.Setup(x => x.CreateLogger(typeof(MyTestInterface).FullName!)).Returns(_logger.Object);
        var aspectConfigurationProviderMock = new Mock<IAspectConfigurationProvider>();
        aspectConfigurationProviderMock
            .Setup(x => x.ShouldIntercept(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<MethodInfo>()))
            .Returns(true);
        var aspectConfigurationProvider = aspectConfigurationProviderMock.Object;
        var aspectConfiguration =
            new AspectConfiguration(new ServiceDescriptor(ITestInterfaceType, MyTestInterface.Type,
                ServiceLifetime.Transient));
        aspectConfiguration.AddEntry(TestAspectFactory.Type, methodsToIntercept: ITestInterfaceType.GetMethods());
        aspectConfiguration.AddEntry(TestAspectFactory2.Type, methodsToIntercept: ITestInterfaceType.GetMethods());
        aspectConfigurationProvider.AddEntry(aspectConfiguration);
        _instance = BaseAspectTestClass<ITestInterface>.Create(new MyTestInterface(), typeof(MyTestInterface),
            loggerFactory.Object, aspectConfigurationProvider);
    }

    /// <summary>
    /// Verifies that an aspect can provide a task result without invoking the target method.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous test.
    /// </returns>
    [Fact]
    public async Task TestCreatingTaskResultWhenMethodNotInvoked()
    {
        // PreInvoke synthesizes a Task result and sets InvokeMethod=false, so the real method is never called.
        // Async + InvokeMethod=false: PreInvoke runs (1 log), inner Invoke is skipped, and PostInvoke does NOT run
        // (outer block gates PostInvoke on !isAsync, and the async wrappers never run because InvokeMethod=false).
        var result = await _instance.GetClassByIdAsync(12);
        _logger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(), It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
        Assert.Equal(new MyUnitTestClass(12, "testing 123"), result);
    }
}
