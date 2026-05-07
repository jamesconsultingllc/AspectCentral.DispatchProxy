using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests.Logging;

public class LoggingAspectFactoryTests
{
    private readonly LoggingAspectFactory _instance;

    public LoggingAspectFactoryTests()
    {
        _instance = new LoggingAspectFactory(new NullLoggerFactory(), new InMemoryAspectConfigurationProvider());
    }

    [Fact]
    public void NullLoggerFactoryThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LoggingAspectFactory(null!, null!));
    }

    [Fact]
    public void NullAspectConfigurationProviderThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LoggingAspectFactory(new NullLoggerFactory(), null!));
    }

    [Fact]
    public void LoggingAspectFactoryConstructorSucceeds()
    {
        new LoggingAspectFactory(new NullLoggerFactory(), new InMemoryAspectConfigurationProvider()).Should()
            .NotBeNull();
    }

    [Fact]
    public void CreateNullInstanceThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _instance.Create(default(MyTestInterface), typeof(MyTestInterface)));
    }

    [Fact]
    public void CreateNullTypeThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _instance.Create(new MyTestInterface(), null!));
    }

    [Fact]
    public void CreatedObjectShouldNotBeNull()
    {
        _instance.Create<ITestInterface>(new MyTestInterface(), typeof(MyTestInterface)).Should().NotBeNull();
    }
}