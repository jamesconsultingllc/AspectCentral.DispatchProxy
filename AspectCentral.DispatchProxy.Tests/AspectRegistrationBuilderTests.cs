// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AspectRegistrationBuilderTests.cs" company="James Consulting LLC">
//   
// </copyright>
//  <summary>
//   The aspect registration builder tests.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy.Logging;
using AspectCentral.DispatchProxy.Profiling;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

/// <summary>
/// Tests fluent aspect registration builder behavior.
/// </summary>
public class AspectRegistrationBuilderTests
{
    /// <summary>
    /// Verifies that multiple aspects can be added to a configured service.
    /// </summary>
    [Fact]
    public void AddAspectSuccess()
    {
        var aspectRegistrationBuilder = new ServiceCollection().AddAspectSupport()
            .AddScoped<ITestInterface, MyTestInterface>()
            .AddLoggingAspect().AddProfilingAspect();
        var aspects = aspectRegistrationBuilder.AspectConfigurationProvider.ConfigurationEntries[0].GetAspects();

        aspects.Count().Should().Be(2);
    }

    /// <summary>
    /// Verifies that adding a non-factory type as an aspect throws <see cref="ArgumentException" />.
    /// </summary>
    [Fact]
    public void AddAspectThrowsArgumentExceptionWhenAspectFactoryDoesNotImplementIAspectFactory()
    {
        var aspectRegistrationBuilder =
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(),
                new InMemoryAspectConfigurationProvider());
        Assert.Throws<ArgumentException>(() => aspectRegistrationBuilder.AddAspect(GetType()));
    }

    /// <summary>
    /// Verifies that adding a null aspect factory type throws <see cref="ArgumentNullException" />.
    /// </summary>
    [Fact]
    public void AddAspectThrowsArgumentNullExceptionWhenAspectFactoryIsNull()
    {
        var aspectRegistrationBuilder =
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(),
                new InMemoryAspectConfigurationProvider());
        Assert.Throws<ArgumentNullException>(() => aspectRegistrationBuilder.AddAspect(default!));
    }

    /// <summary>
    /// Verifies that aspects cannot be added after services are locked in the builder.
    /// </summary>
    [Fact]
    public void AddAspectThrowsInvalidOperationExceptionWhenServicesHaveBeenRegistered()
    {
        var aspectRegistrationBuilder =
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(),
                new InMemoryAspectConfigurationProvider());
        Assert.Throws<InvalidOperationException>(() =>
            aspectRegistrationBuilder.AddAspect(LoggingAspectFactory.LoggingAspectFactoryType));
    }

    /// <summary>
    /// Verifies that an aspect factory can be added with an explicit method filter.
    /// </summary>
    [Fact]
    public void AddAspectWithFactorySuccess()
    {
        var aspectRegistrationBuilder = new ServiceCollection().AddAspectSupport().AddService(typeof(ITestInterface),
                serviceProvider => new MyTestInterface(), ServiceLifetime.Scoped)
            .AddAspect(LoggingAspectFactory.LoggingAspectFactoryType, null, typeof(MyTestInterface).GetMethods());
        var aspects = aspectRegistrationBuilder.AspectConfigurationProvider.ConfigurationEntries[0].GetAspects();
        aspects.Count().Should().Be(1);
    }

    /// <summary>
    /// Verifies that a service registered through the builder is added to configuration.
    /// </summary>
    [Fact]
    public void AddServiceSuccess()
    {
        var aspectRegistrationBuilder =
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(),
                new InMemoryAspectConfigurationProvider());
        aspectRegistrationBuilder.AddService(typeof(IAspectFactory), LoggingAspectFactory.LoggingAspectFactoryType,
            ServiceLifetime.Scoped);
        aspectRegistrationBuilder.Services.Count.Should().Be(2);
        aspectRegistrationBuilder.AspectConfigurationProvider.ConfigurationEntries.Count.Should().Be(1);
        aspectRegistrationBuilder.AspectConfigurationProvider.ConfigurationEntries[0].ServiceDescriptor
            .ImplementationType.Should().Be(LoggingAspectFactory.LoggingAspectFactoryType);
    }

    /// <summary>
    /// Verifies that a service registration rejects implementation types that do not implement the service type.
    /// </summary>
    [Fact]
    public void AddServiceThrowsArgumentNullExceptionWhenImplementationDoesNotImplementService()
    {
        var aspectRegistrationBuilder =
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(),
                new InMemoryAspectConfigurationProvider());
        Assert.Throws<ArgumentException>(() =>
            aspectRegistrationBuilder.AddService(typeof(IAspectConfigurationProvider), GetType(),
                ServiceLifetime.Scoped));
    }

    /// <summary>
    /// Verifies that a service registration rejects a null implementation type.
    /// </summary>
    [Fact]
    public void AddServiceThrowsArgumentNullExceptionWhenImplementationIsNull()
    {
        var aspectRegistrationBuilder =
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(),
                new InMemoryAspectConfigurationProvider());
        Assert.Throws<ArgumentNullException>(() =>
            aspectRegistrationBuilder.AddService(typeof(IAspectConfigurationProvider), default(Type)!,
                ServiceLifetime.Scoped));
    }

    /// <summary>
    /// Verifies that a service registration rejects a null service type.
    /// </summary>
    [Fact]
    public void AddServiceThrowsArgumentNullExceptionWhenServiceIsNull()
    {
        var aspectRegistrationBuilder =
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(),
                new InMemoryAspectConfigurationProvider());
        Assert.Throws<ArgumentNullException>(() =>
            aspectRegistrationBuilder.AddService(null!, default(Type)!, ServiceLifetime.Scoped));
    }

    /// <summary>
    /// Verifies that a factory-based service registration is added to configuration.
    /// </summary>
    [Fact]
    public void AddServiceWithFactorySuccess()
    {
        var aspectRegistrationBuilder =
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(),
                new InMemoryAspectConfigurationProvider());
        aspectRegistrationBuilder.AddService(
            typeof(IAspectFactory),
            provider => new LoggingAspectFactory(provider.GetService<ILoggerFactory>()!,
                provider.GetService<IAspectConfigurationProvider>()!),
            ServiceLifetime.Scoped);
        aspectRegistrationBuilder.Services.Count.Should().Be(1);
        aspectRegistrationBuilder.AspectConfigurationProvider.ConfigurationEntries.Count.Should().Be(1);
        aspectRegistrationBuilder.AspectConfigurationProvider.ConfigurationEntries[0].ServiceDescriptor
            .ImplementationFactory.Should().NotBeNull();
        aspectRegistrationBuilder.AspectConfigurationProvider.ConfigurationEntries[0].ServiceDescriptor
            .ImplementationType.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a factory-based service registration rejects a null implementation factory.
    /// </summary>
    [Fact]
    public void AddServiceWithFactoryThrowsArgumentNullExceptionWhenImplementationIsNull()
    {
        var aspectRegistrationBuilder =
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(),
                new InMemoryAspectConfigurationProvider());
        Assert.Throws<ArgumentNullException>(() => aspectRegistrationBuilder.AddService(typeof(IAspectFactory),
            default(Func<IServiceProvider, object>)!, ServiceLifetime.Scoped));
    }

    /// <summary>
    /// Verifies that a factory-based service registration rejects a null service type.
    /// </summary>
    [Fact]
    public void AddServiceWithFactoryThrowsArgumentNullExceptionWhenServiceIsNull()
    {
        var aspectRegistrationBuilder =
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(),
                new InMemoryAspectConfigurationProvider());
        Assert.Throws<ArgumentNullException>(() =>
            aspectRegistrationBuilder.AddService(null!, default(Func<IServiceProvider, object>)!,
                ServiceLifetime.Scoped));
    }

    /// <summary>
    /// Verifies that the registration builder can be constructed with valid dependencies.
    /// </summary>
    [Fact]
    public void ConstructorCreatesNewObject()
    {
        var aspectRegistrationBuilder =
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(),
                new InMemoryAspectConfigurationProvider());
        aspectRegistrationBuilder.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the constructor rejects a null aspect configuration provider.
    /// </summary>
    [Fact]
    public void ConstructorThrowsArgumentNullExceptionWhenAspectConfigurationProviderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DispatchProxyAspectRegistrationBuilder(new ServiceCollection(), null!));
    }

    /// <summary>
    /// Verifies that the constructor rejects a null service collection.
    /// </summary>
    [Fact]
    public void ConstructorThrowsArgumentNullExceptionWhenServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new DispatchProxyAspectRegistrationBuilder(null!, null!));
    }
}
