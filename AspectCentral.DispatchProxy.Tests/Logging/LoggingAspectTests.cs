// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingAspectTests.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The logging aspect tests.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests.Logging;

/// <summary>
/// Tests the built-in logging aspect.
/// </summary>
public class LoggingAspectTests
{
    /// <summary>
    /// Aspect configuration provider used to enable logging for all test interface methods.
    /// </summary>
    private readonly IAspectConfigurationProvider _aspectConfigurationProvider;

    /// <summary>
    /// Proxied test service used by the logging assertions.
    /// </summary>
    private readonly ITestInterface _instance;

    /// <summary>
    /// Mock logger that receives generated logging aspect entries.
    /// </summary>
    private readonly Mock<ILogger> _logger;

    /// <summary>
    /// Mock logger factory used to supply <see cref="_logger" /> to the aspect.
    /// </summary>
    private readonly Mock<ILoggerFactory> _loggerFactory;

    /// <summary>
    /// Initializes a logging proxy over the test service.
    /// </summary>
    public LoggingAspectTests()
    {
        _loggerFactory = new Mock<ILoggerFactory>();
        _logger = new Mock<ILogger>();
        _aspectConfigurationProvider = new InMemoryAspectConfigurationProvider();
        var aspectConfiguration = new AspectConfiguration(new ServiceDescriptor(AspectRegistrationTests.InterfaceType,
            AspectRegistrationTests.MyTestInterfaceType, ServiceLifetime.Transient));
        aspectConfiguration.AddEntry(LoggingAspectFactory.LoggingAspectFactoryType,
            methodsToIntercept: AspectRegistrationTests.InterfaceType.GetMethods());
        aspectConfiguration.AddEntry(LoggingAspectFactory.LoggingAspectFactoryType,
            methodsToIntercept: AspectRegistrationTests.InterfaceType.GetMethods());
        _aspectConfigurationProvider.AddEntry(aspectConfiguration);
        _loggerFactory.Setup(x => x.CreateLogger(typeof(MyTestInterface).FullName!)).Returns(_logger.Object);
        _logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _instance = LoggingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            typeof(MyTestInterface),
            _loggerFactory.Object,
            _aspectConfigurationProvider,
            LoggingAspectFactory.LoggingAspectFactoryType);
    }

    /// <summary>
    /// Verifies that a synchronous intercepted call logs start and end entries.
    /// </summary>
    [Fact]
    public void MyTestMethod()
    {
        _instance.Test(1, "2", new MyUnitTestClass(1, "2"));
        _logger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(), It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Exactly(2));
    }

    /// <summary>
    /// Verifies that an asynchronous action logs start and end entries after completion.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous test.
    /// </returns>
    [Fact]
    public async Task TestLoggingAsync()
    {
        await _instance.TestAsync(1, "2", null!);
        _logger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(), It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Exactly(2));
    }

    /// <summary>
    /// Verifies that an asynchronous function logs start, return value, and end entries.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous test.
    /// </returns>
    [Fact]
    public async Task TestLoggingAsyncWithResult()
    {
        await _instance.GetClassByIdAsync(1);
        _logger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(), It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Exactly(3));
    }

    [Fact]
    public void CreateNullInstanceThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LoggingAspect<ITestInterface>.Create(
            null!,
            typeof(MyTestInterface),
            _loggerFactory.Object,
            _aspectConfigurationProvider,
            LoggingAspectFactory.LoggingAspectFactoryType));
    }

    [Fact]
    public void CreateNullTypeThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LoggingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            null!,
            _loggerFactory.Object,
            _aspectConfigurationProvider,
            LoggingAspectFactory.LoggingAspectFactoryType));
    }

    [Fact]
    public void CreateNullLoggerThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LoggingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            typeof(MyTestInterface),
            null!,
            _aspectConfigurationProvider,
            LoggingAspectFactory.LoggingAspectFactoryType));
    }

    [Fact]
    public void CreateNullAspectConfigurationProviderThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LoggingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            typeof(MyTestInterface),
            _loggerFactory.Object,
            null!,
            LoggingAspectFactory.LoggingAspectFactoryType));
    }

    [Fact]
    public void CreateNullFactoryTypeThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LoggingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            typeof(MyTestInterface),
            _loggerFactory.Object,
            _aspectConfigurationProvider,
            null!));
    }
}
