// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfilingAspectTests.cs" company="James Consulting LLC">
//   
// </copyright>
// // <summary>
//   The profiling aspect tests.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy.Logging;
using AspectCentral.DispatchProxy.Profiling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests.Profiling;

/// <summary>
/// Tests the built-in profiling aspect.
/// </summary>
public class ProfilingAspectTests
{
    /// <summary>
    /// Aspect configuration provider used to enable profiling for all test interface methods.
    /// </summary>
    private readonly IAspectConfigurationProvider _aspectConfigurationProvider;

    /// <summary>
    /// Proxied test service used by the profiling assertions.
    /// </summary>
    private readonly ITestInterface _instance;

    /// <summary>
    /// Mock logger that receives generated profiling aspect entries.
    /// </summary>
    private readonly Mock<ILogger> _logger;

    /// <summary>
    /// Mock logger factory used to supply <see cref="_logger" /> to the aspect.
    /// </summary>
    private readonly Mock<ILoggerFactory> _loggerFactory;

    /// <summary>
    /// Initializes a profiling proxy over the test service.
    /// </summary>
    public ProfilingAspectTests()
    {
        _loggerFactory = new Mock<ILoggerFactory>();
        _logger = new Mock<ILogger>();
        _aspectConfigurationProvider = new InMemoryAspectConfigurationProvider();
        var aspectConfiguration = new AspectConfiguration(new ServiceDescriptor(AspectRegistrationTests.InterfaceType,
            AspectRegistrationTests.MyTestInterfaceType, ServiceLifetime.Transient));
        aspectConfiguration.AddEntry(LoggingAspectFactory.LoggingAspectFactoryType,
            methodsToIntercept: AspectRegistrationTests.InterfaceType.GetMethods());
        aspectConfiguration.AddEntry(ProfilingAspectFactory.ProfilingAspectFactoryType,
            methodsToIntercept: AspectRegistrationTests.InterfaceType.GetMethods());
        _aspectConfigurationProvider.AddEntry(aspectConfiguration);
        _loggerFactory.Setup(x => x.CreateLogger(typeof(MyTestInterface).FullName!)).Returns(_logger.Object);
        _logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _instance = ProfilingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            typeof(MyTestInterface),
            _loggerFactory.Object,
            _aspectConfigurationProvider,
            ProfilingAspectFactory.ProfilingAspectFactoryType);
    }

    /// <summary>
    /// Verifies that an asynchronous action logs profiling start and duration entries.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous test.
    /// </returns>
    [Fact]
    public async Task ProfilingAsync()
    {
        await _instance.TestAsync(1, "2", null!);
        _logger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(), It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Exactly(2));
    }

    /// <summary>
    /// Verifies that an asynchronous function logs profiling start and duration entries.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous test.
    /// </returns>
    [Fact]
    public async Task ProfilingAsyncWithResult()
    {
        await _instance.GetClassByIdAsync(1);
        _logger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(), It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Exactly(2));
    }

    /// <summary>
    /// Verifies that a synchronous call logs profiling start and duration entries.
    /// </summary>
    [Fact]
    public void ProfilingSyncMethod()
    {
        _instance.Test(1, "2", new MyUnitTestClass(1, "2"));
        _logger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(), It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Exactly(2));
    }

    [Fact]
    public void CreateNullInstanceThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ProfilingAspect<ITestInterface>.Create(
            null!,
            typeof(MyTestInterface),
            _loggerFactory.Object,
            _aspectConfigurationProvider,
            ProfilingAspectFactory.ProfilingAspectFactoryType));
    }

    [Fact]
    public void CreateNullTypeThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ProfilingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            null!,
            _loggerFactory.Object,
            _aspectConfigurationProvider,
            ProfilingAspectFactory.ProfilingAspectFactoryType));
    }

    [Fact]
    public void CreateNullLoggerThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ProfilingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            typeof(MyTestInterface),
            null!,
            _aspectConfigurationProvider,
            ProfilingAspectFactory.ProfilingAspectFactoryType));
    }

    [Fact]
    public void CreateNullAspectConfigurationProviderThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ProfilingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            typeof(MyTestInterface),
            _loggerFactory.Object,
            null!,
            ProfilingAspectFactory.ProfilingAspectFactoryType));
    }

    [Fact]
    public void CreateNullFactoryTypeThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ProfilingAspect<ITestInterface>.Create(
            new MyTestInterface(),
            typeof(MyTestInterface),
            _loggerFactory.Object,
            _aspectConfigurationProvider,
            null!));
    }
}
