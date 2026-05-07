//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="ServiceCollectionExtensionsTests.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions.Configuration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

public class ServiceCollectionExtensionsTests
{
    private readonly Mock<IAspectConfigurationProvider> _aspectConfigurationProviderMock;
    private readonly ServiceCollection _serviceCollection;

    public ServiceCollectionExtensionsTests()
    {
        _serviceCollection = new ServiceCollection();
        _aspectConfigurationProviderMock = new Mock<IAspectConfigurationProvider>();
    }

    [Fact]
    public void AddAspectSupportThrowsArgumentNullExceptionWhenServiceCollectionIsNull()
    {
        Assert.Throws<ArgumentNullException>("serviceCollection",
            () => default(IServiceCollection)!.AddAspectSupport(default(IAspectConfigurationProvider)!));
    }

    [Fact]
    public void AddAspectSupportThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>("aspectConfigurationProvider",
            () => _serviceCollection.AddAspectSupport(default(IAspectConfigurationProvider)!));
    }

    [Fact]
    public void AddAspectSupportRegistersProviderAndFactoriesAndInterceptors()
    {
        _aspectConfigurationProviderMock
            .Setup(x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface))).Returns(
                new AspectConfiguration(ServiceDescriptor.Describe(typeof(ITestInterface), typeof(MyTestInterface),
                    ServiceLifetime.Transient)));
        _serviceCollection.TryAddTransient<ITestInterface, MyTestInterface>();
        _serviceCollection.AddAspectSupport(_aspectConfigurationProviderMock.Object);
        _aspectConfigurationProviderMock.Verify(
            x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface)), Times.Once);
        _serviceCollection.Count.Should().Be(7);
        _serviceCollection.Count(x => x.ServiceType == typeof(IAspectConfigurationProvider)).Should().Be(1);
        _serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory)).Should().Be(1);
        _serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory2)).Should().Be(1);
        _serviceCollection.Count(x => x.ServiceType == typeof(MyTestInterface)).Should().Be(1);
        _serviceCollection.Count(x => x.ServiceType == typeof(ITestInterface) && x.ImplementationFactory != null)
            .Should().Be(1);
    }

    [Fact]
    public void AddAspectSupportDoesNotReplaceServiceDescriptorsThatAreNotConfigured()
    {
        _aspectConfigurationProviderMock
            .Setup(x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface)))
            .Returns(default(AspectConfiguration));
        _serviceCollection.TryAddTransient<ITestInterface, MyTestInterface>();
        _serviceCollection.AddAspectSupport(_aspectConfigurationProviderMock.Object);
        _aspectConfigurationProviderMock.Verify(
            x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface)), Times.Once);
        _serviceCollection.Count.Should().Be(6);
        _serviceCollection.Count(x => x.ServiceType == typeof(IAspectConfigurationProvider)).Should().Be(1);
        _serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory)).Should().Be(1);
        _serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory2)).Should().Be(1);
    }

    [Fact]
    public void EnsureServicesAreProperlyResolvedWithFactory()
    {
        var configuration = new AspectConfiguration(ServiceDescriptor.Describe(typeof(ITestInterface),
            typeof(MyTestInterface), ServiceLifetime.Transient));
        configuration.AddEntry(TestAspectFactory.Type);
        _aspectConfigurationProviderMock
            .Setup(x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface)))
            .Returns(configuration);
        _serviceCollection.AddLogging();
        _serviceCollection.TryAddTransient<ITestInterface, MyTestInterface>();
        _serviceCollection.AddAspectSupport(_aspectConfigurationProviderMock.Object);
        _aspectConfigurationProviderMock.Verify(
            x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface)), Times.Once);
        _serviceCollection.BuildServiceProvider().GetService<ITestInterface>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigureAspectsLeavesOpenGenericDescriptorsUntouched()
    {
        // Open generic registrations cannot be safely rewritten to a closed factory descriptor
        // by ConfigureAspects; they must be left for DI's open-generic resolver to handle.
        _serviceCollection.AddTransient(typeof(IGenericService<>), typeof(GenericService<>));
        var beforeDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(IGenericService<>));

        _serviceCollection.AddAspectSupport(_aspectConfigurationProviderMock.Object);

        var afterDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(IGenericService<>));
        afterDescriptor.Should().BeSameAs(beforeDescriptor);
        afterDescriptor.ImplementationType.Should().Be(typeof(GenericService<>));
        afterDescriptor.ImplementationFactory.Should().BeNull();
        // Provider should never be queried for an open generic.
        _aspectConfigurationProviderMock.Verify(
            x => x.GetTypeAspectConfiguration(It.IsAny<Type>(), It.IsAny<Type>()),
            Times.Never);
    }

    [Fact]
    public void ConfigureAspectsLeavesImplementationFactoryDescriptorsUntouched()
    {
        // Factory-based registrations are not proxied via the static rewrite path because doing so
        // safely requires preserving DI ownership/disposal semantics for the factory's product.
        _serviceCollection.AddTransient<ITestInterface>(_ => new MyTestInterface());
        var beforeDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(ITestInterface));

        _serviceCollection.AddAspectSupport(_aspectConfigurationProviderMock.Object);

        var afterDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(ITestInterface));
        afterDescriptor.Should().BeSameAs(beforeDescriptor);
        afterDescriptor.ImplementationFactory.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureAspectsLeavesImplementationInstanceDescriptorsUntouched()
    {
        // Instance registrations are externally owned by the caller. Rewriting them to a factory
        // descriptor would transfer disposal ownership to the DI container, changing behavior.
        var instance = new MyTestInterface();
        _serviceCollection.AddSingleton<ITestInterface>(instance);
        var beforeDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(ITestInterface));

        _serviceCollection.AddAspectSupport(_aspectConfigurationProviderMock.Object);

        var afterDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(ITestInterface));
        afterDescriptor.Should().BeSameAs(beforeDescriptor);
        afterDescriptor.ImplementationInstance.Should().BeSameAs(instance);
    }

    [Fact]
    public void AddAspectSupportThrowsWhenAdifferentProviderIsAlreadyRegistered()
    {
        var firstProvider = new Mock<IAspectConfigurationProvider>().Object;
        var secondProvider = new Mock<IAspectConfigurationProvider>().Object;
        _serviceCollection.AddSingleton(firstProvider);

        Action act = () => _serviceCollection.AddAspectSupport(secondProvider);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*different instance*");
    }

    [Fact]
    public void AddAspectSupportSucceedsWhenSameProviderIsAlreadyRegistered()
    {
        _serviceCollection.AddSingleton(_aspectConfigurationProviderMock.Object);

        Action act = () => _serviceCollection.AddAspectSupport(_aspectConfigurationProviderMock.Object);

        act.Should().NotThrow();
        _serviceCollection.Count(x => x.ServiceType == typeof(IAspectConfigurationProvider)).Should().Be(1);
    }

    [Fact]
    public void ConfigureAspectsThrowsWhenConcreteTypeAlreadyRegisteredWithDifferentLifetime()
    {
        // Pre-register the impl as Singleton, then register the interface as Transient with an
        // aspect configured. The proxy factory needs the impl, so DI semantics demand the impl be
        // registered with the same lifetime as the interface — otherwise the proxy resolves a
        // concrete instance with the wrong lifetime.
        _aspectConfigurationProviderMock
            .Setup(x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface)))
            .Returns(new AspectConfiguration(ServiceDescriptor.Describe(
                typeof(ITestInterface), typeof(MyTestInterface), ServiceLifetime.Transient)));
        _serviceCollection.AddSingleton<MyTestInterface>();
        _serviceCollection.AddTransient<ITestInterface, MyTestInterface>();

        Action act = () => _serviceCollection.AddAspectSupport(_aspectConfigurationProviderMock.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered with lifetime Singleton*Transient*");
    }

    [Fact]
    public void ConfigureAspectsAcceptsExistingConcreteRegistrationWithMatchingLifetime()
    {
        _aspectConfigurationProviderMock
            .Setup(x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface)))
            .Returns(new AspectConfiguration(ServiceDescriptor.Describe(
                typeof(ITestInterface), typeof(MyTestInterface), ServiceLifetime.Transient)));
        _serviceCollection.AddTransient<MyTestInterface>();
        _serviceCollection.AddTransient<ITestInterface, MyTestInterface>();

        Action act = () => _serviceCollection.AddAspectSupport(_aspectConfigurationProviderMock.Object);

        act.Should().NotThrow();
    }
}

internal interface IGenericService<T>
{
    T Echo(T value);
}

internal class GenericService<T> : IGenericService<T>
{
    public T Echo(T value)
    {
        return value;
    }
}