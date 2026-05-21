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
using NSubstitute;
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
    /// Recording logger that receives generated logging aspect entries.
    /// </summary>
    private readonly RecordingLogger _logger;

    /// <summary>
    /// Logger factory substitute used to supply <see cref="_logger" /> to the aspect.
    /// </summary>
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a logging proxy over the test service.
    /// </summary>
    public LoggingAspectTests()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _logger = new RecordingLogger();
        _aspectConfigurationProvider = new InMemoryAspectConfigurationProvider();
        var aspectConfiguration = new AspectConfiguration(new ServiceDescriptor(AspectRegistrationTests.InterfaceType,
            AspectRegistrationTests.MyTestInterfaceType, ServiceLifetime.Transient));
        aspectConfiguration.AddEntry(LoggingAspectFactory.LoggingAspectFactoryType,
            methodsToIntercept: AspectRegistrationTests.InterfaceType.GetMethods());
        _aspectConfigurationProvider.AddEntry(aspectConfiguration);
        _loggerFactory.CreateLogger(typeof(MyTestInterface).FullName!).Returns(_logger);
        _instance = LoggingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            typeof(MyTestInterface),
            _loggerFactory,
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
        Assert.Equal(2, _logger.CountAt(LogLevel.Information));
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
        Assert.Equal(2, _logger.CountAt(LogLevel.Information));
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
        Assert.Equal(3, _logger.CountAt(LogLevel.Information));
    }

    [Fact]
    public void CreateNullInstanceThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LoggingAspect<ITestInterface>.Create(
            null!,
            typeof(MyTestInterface),
            _loggerFactory,
            _aspectConfigurationProvider,
            LoggingAspectFactory.LoggingAspectFactoryType));
    }

    [Fact]
    public void CreateNullTypeThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LoggingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            null!,
            _loggerFactory,
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
            _loggerFactory,
            null!,
            LoggingAspectFactory.LoggingAspectFactoryType));
    }

    [Fact]
    public void CreateNullFactoryTypeThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LoggingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            typeof(MyTestInterface),
            _loggerFactory,
            _aspectConfigurationProvider,
            null!));
    }
}
