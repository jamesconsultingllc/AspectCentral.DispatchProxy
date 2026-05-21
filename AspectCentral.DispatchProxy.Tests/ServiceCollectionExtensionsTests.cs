//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="ServiceCollectionExtensionsTests.cs" company="James Consulting LLC">
//    Copyright (c) 2019 James Consulting LLC. Licensed under the MIT License.
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests;

public class ServiceCollectionExtensionsTests
{
    private readonly IAspectConfigurationProvider _aspectConfigurationProvider;
    private readonly ServiceCollection _serviceCollection;

    public ServiceCollectionExtensionsTests()
    {
        _serviceCollection = new ServiceCollection();
        _aspectConfigurationProvider = Substitute.For<IAspectConfigurationProvider>();
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
        _aspectConfigurationProvider
            .GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface))
            .Returns(new AspectConfiguration(ServiceDescriptor.Describe(typeof(ITestInterface),
                typeof(MyTestInterface), ServiceLifetime.Transient)));
        _serviceCollection.TryAddTransient<ITestInterface, MyTestInterface>();
        _serviceCollection.AddAspectSupport(_aspectConfigurationProvider);
        _aspectConfigurationProvider.Received(1)
            .GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface));
        Assert.Equal(1, _serviceCollection.Count(x => x.ServiceType == typeof(IAspectConfigurationProvider)));
        Assert.Equal(1, _serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory)));
        Assert.Equal(1, _serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory2)));
        Assert.Equal(1, _serviceCollection.Count(x => x.ServiceType == typeof(MyTestInterface)));
        Assert.Equal(1,
            _serviceCollection.Count(x =>
                x.ServiceType == typeof(ITestInterface) && x.ImplementationFactory != null));
    }

    [Fact]
    public void AddAspectSupportDoesNotReplaceServiceDescriptorsThatAreNotConfigured()
    {
        _aspectConfigurationProvider
            .GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface))
            .Returns(default(AspectConfiguration));
        _serviceCollection.TryAddTransient<ITestInterface, MyTestInterface>();
        _serviceCollection.AddAspectSupport(_aspectConfigurationProvider);
        _aspectConfigurationProvider.Received(1)
            .GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface));
        Assert.Equal(1, _serviceCollection.Count(x => x.ServiceType == typeof(IAspectConfigurationProvider)));
        Assert.Equal(1, _serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory)));
        Assert.Equal(1, _serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory2)));
        Assert.Equal(1, _serviceCollection.Count(x => x.ServiceType == typeof(ITestInterface)));
        Assert.Equal(typeof(MyTestInterface),
            _serviceCollection.Single(x => x.ServiceType == typeof(ITestInterface)).ImplementationType);
    }

    [Fact]
    public void EnsureServicesAreProperlyResolvedWithFactory()
    {
        var configuration = new AspectConfiguration(ServiceDescriptor.Describe(typeof(ITestInterface),
            typeof(MyTestInterface), ServiceLifetime.Transient));
        configuration.AddEntry(TestAspectFactory.Type);
        _aspectConfigurationProvider
            .GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface))
            .Returns(configuration);
        _serviceCollection.AddLogging();
        _serviceCollection.TryAddTransient<ITestInterface, MyTestInterface>();
        _serviceCollection.AddAspectSupport(_aspectConfigurationProvider);
        _aspectConfigurationProvider.Received(1)
            .GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface));
        Assert.NotNull(_serviceCollection.BuildServiceProvider().GetService<ITestInterface>());
    }

    [Fact]
    public void ConfigureAspectsLeavesOpenGenericDescriptorsUntouched()
    {
        // Open generic registrations cannot be safely rewritten to a closed factory descriptor
        // by ConfigureAspects; they must be left for DI's open-generic resolver to handle.
        _serviceCollection.AddTransient(typeof(IGenericService<>), typeof(GenericService<>));
        var beforeDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(IGenericService<>));

        _serviceCollection.AddAspectSupport(_aspectConfigurationProvider);

        var afterDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(IGenericService<>));
        Assert.Same(beforeDescriptor, afterDescriptor);
        Assert.Equal(typeof(GenericService<>), afterDescriptor.ImplementationType);
        Assert.Null(afterDescriptor.ImplementationFactory);
        // Provider should never be queried for an open generic.
        _aspectConfigurationProvider.DidNotReceive()
            .GetTypeAspectConfiguration(Arg.Any<Type>(), Arg.Any<Type>());
    }

    [Fact]
    public void ConfigureAspectsLeavesImplementationFactoryDescriptorsUntouched()
    {
        // Factory-based registrations are not proxied via the static rewrite path because doing so
        // safely requires preserving DI ownership/disposal semantics for the factory's product.
        _serviceCollection.AddTransient<ITestInterface>(_ => new MyTestInterface());
        var beforeDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(ITestInterface));

        _serviceCollection.AddAspectSupport(_aspectConfigurationProvider);

        var afterDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(ITestInterface));
        Assert.Same(beforeDescriptor, afterDescriptor);
        Assert.NotNull(afterDescriptor.ImplementationFactory);
    }

    [Fact]
    public void ConfigureAspectsLeavesImplementationInstanceDescriptorsUntouched()
    {
        // Instance registrations are externally owned by the caller. Rewriting them to a factory
        // descriptor would transfer disposal ownership to the DI container, changing behavior.
        var instance = new MyTestInterface();
        _serviceCollection.AddSingleton<ITestInterface>(instance);
        var beforeDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(ITestInterface));

        _serviceCollection.AddAspectSupport(_aspectConfigurationProvider);

        var afterDescriptor = _serviceCollection.Single(d => d.ServiceType == typeof(ITestInterface));
        Assert.Same(beforeDescriptor, afterDescriptor);
        Assert.Same(instance, afterDescriptor.ImplementationInstance);
    }

    [Fact]
    public void AddAspectSupportThrowsWhenADifferentProviderIsAlreadyRegistered()
    {
        var firstProvider = Substitute.For<IAspectConfigurationProvider>();
        var secondProvider = Substitute.For<IAspectConfigurationProvider>();
        _serviceCollection.AddSingleton(firstProvider);

        var ex = Assert.Throws<InvalidOperationException>(
            () => _serviceCollection.AddAspectSupport(secondProvider));
        Assert.Contains("already registered", ex.Message);
        Assert.Contains("different instance", ex.Message);
    }

    [Fact]
    public void AddAspectSupportSucceedsWhenSameProviderIsAlreadyRegistered()
    {
        _serviceCollection.AddSingleton(_aspectConfigurationProvider);

        // Should not throw.
        _serviceCollection.AddAspectSupport(_aspectConfigurationProvider);
        Assert.Equal(1, _serviceCollection.Count(x => x.ServiceType == typeof(IAspectConfigurationProvider)));
    }

    [Fact]
    public void AddAspectSupportIgnoresKeyedProviderRegistration()
    {
        // A keyed IAspectConfigurationProvider is not what GetService<IAspectConfigurationProvider>()
        // resolves; it must not trigger a provider-mismatch throw against a different non-keyed instance.
        var keyedProvider = Substitute.For<IAspectConfigurationProvider>();
        var newProvider = Substitute.For<IAspectConfigurationProvider>();
        _serviceCollection.AddKeyedSingleton<IAspectConfigurationProvider>("aux", keyedProvider);

        // Should not throw.
        _serviceCollection.AddAspectSupport(newProvider);

        // The keyed registration is left intact and the new provider is added as a non-keyed singleton.
        Assert.Equal(1,
            _serviceCollection.Count(d => d.ServiceType == typeof(IAspectConfigurationProvider) && !d.IsKeyedService));
        Assert.Equal(1,
            _serviceCollection.Count(d => d.ServiceType == typeof(IAspectConfigurationProvider) && d.IsKeyedService));
    }

    [Fact]
    public void AddAspectSupportFluentIgnoresKeyedProviderRegistration()
    {
        // Same scenario for the fluent overload that calls GetOrAddInMemoryProvider — a keyed
        // provider must not be reused or cause the throw path.
        var keyedProvider = Substitute.For<IAspectConfigurationProvider>();
        _serviceCollection.AddKeyedSingleton<IAspectConfigurationProvider>("aux", keyedProvider);

        // Should not throw.
        _serviceCollection.AddAspectSupport();

        // A new non-keyed InMemoryAspectConfigurationProvider is registered alongside the keyed one.
        Assert.Equal(1,
            _serviceCollection.Count(d => d.ServiceType == typeof(IAspectConfigurationProvider) && !d.IsKeyedService));
        Assert.Equal(1,
            _serviceCollection.Count(d => d.ServiceType == typeof(IAspectConfigurationProvider) && d.IsKeyedService));
    }

    [Fact]
    public void ConfigureAspectsThrowsWhenConcreteTypeAlreadyRegisteredWithDifferentLifetime()
    {
        // Pre-register the impl as Singleton, then register the interface as Transient with an
        // aspect configured. The proxy factory needs the impl, so DI semantics demand the impl be
        // registered with the same lifetime as the interface — otherwise the proxy resolves a
        // concrete instance with the wrong lifetime.
        _aspectConfigurationProvider
            .GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface))
            .Returns(new AspectConfiguration(ServiceDescriptor.Describe(
                typeof(ITestInterface), typeof(MyTestInterface), ServiceLifetime.Transient)));
        _serviceCollection.AddSingleton<MyTestInterface>();
        _serviceCollection.AddTransient<ITestInterface, MyTestInterface>();

        var ex = Assert.Throws<InvalidOperationException>(
            () => _serviceCollection.AddAspectSupport(_aspectConfigurationProvider));
        Assert.Contains("already registered with lifetime Singleton", ex.Message);
        Assert.Contains("Transient", ex.Message);
    }

    [Fact]
    public void ConfigureAspectsAcceptsExistingConcreteRegistrationWithMatchingLifetime()
    {
        _aspectConfigurationProvider
            .GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface))
            .Returns(new AspectConfiguration(ServiceDescriptor.Describe(
                typeof(ITestInterface), typeof(MyTestInterface), ServiceLifetime.Transient)));
        _serviceCollection.AddTransient<MyTestInterface>();
        _serviceCollection.AddTransient<ITestInterface, MyTestInterface>();

        // Should not throw.
        _serviceCollection.AddAspectSupport(_aspectConfigurationProvider);
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
