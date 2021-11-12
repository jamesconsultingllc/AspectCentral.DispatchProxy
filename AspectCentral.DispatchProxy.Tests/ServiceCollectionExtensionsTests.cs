//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="ServiceCollectionExtensionsTests.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using AspectCentral.Abstractions.Configuration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        private readonly ServiceCollection serviceCollection;
        private readonly Mock<IAspectConfigurationProvider> aspectConfigurationProviderMock;

        public ServiceCollectionExtensionsTests()
        {
            serviceCollection = new ServiceCollection();
            aspectConfigurationProviderMock = new Mock<IAspectConfigurationProvider>();
        }
        
        [Fact]
        public void AddAspectSupportThrowsArgumentNullExceptionWhenServiceCollectionIsNull()
        {
            Assert.Throws<ArgumentNullException>("serviceCollection",() => default(IServiceCollection).AddAspectSupport(default(IAspectConfigurationProvider)));
        }

        [Fact]
        public void AddAspectSupportThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("aspectConfigurationProvider",() => serviceCollection.AddAspectSupport(default(IAspectConfigurationProvider)));
        }

        [Fact]
        public void AddAspectSupportRegistersProviderAndFactoriesAndInterceptors()
        {
            aspectConfigurationProviderMock.Setup(x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface))).Returns(new AspectConfiguration(ServiceDescriptor.Describe(typeof(ITestInterface), typeof(MyTestInterface), ServiceLifetime.Transient)));
            serviceCollection.TryAddTransient<ITestInterface, MyTestInterface>();
            serviceCollection.AddAspectSupport(aspectConfigurationProviderMock.Object);
            aspectConfigurationProviderMock.Verify(x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface)), Times.Once);
            serviceCollection.Count.Should().Be(7);
            serviceCollection.Count(x => x.ServiceType == typeof(IAspectConfigurationProvider)).Should().Be(1);
            serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory)).Should().Be(1);
            serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory2)).Should().Be(1);
            serviceCollection.Count(x => x.ServiceType == typeof(MyTestInterface)).Should().Be(1);
            serviceCollection.Count(x => x.ServiceType == typeof(ITestInterface) && x.ImplementationFactory != null).Should().Be(1);
        }
        
        [Fact]
        public void AddAspectSupportDoesNotReplaceServiceDescriptorsThatAreNotConfigured()
        {
            aspectConfigurationProviderMock.Setup(x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface))).Returns(default(AspectConfiguration));
            serviceCollection.TryAddTransient<ITestInterface, MyTestInterface>();
            serviceCollection.AddAspectSupport(aspectConfigurationProviderMock.Object);
            aspectConfigurationProviderMock.Verify(x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface)), Times.Once);
            serviceCollection.Count.Should().Be(6);
            serviceCollection.Count(x => x.ServiceType == typeof(IAspectConfigurationProvider)).Should().Be(1);
            serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory)).Should().Be(1);
            serviceCollection.Count(x => x.ServiceType == typeof(TestAspectFactory2)).Should().Be(1);
        }

        [Fact]
        public void EnsureServicesAreProperlyResolvedWithFactory()
        {
            var configuration = new AspectConfiguration(ServiceDescriptor.Describe(typeof(ITestInterface), typeof(MyTestInterface), ServiceLifetime.Transient));
            configuration.AddEntry(TestAspectFactory.Type);
            aspectConfigurationProviderMock.Setup(x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface))).Returns(configuration);
            serviceCollection.AddLogging();
            serviceCollection.TryAddTransient<ITestInterface, MyTestInterface>();
            serviceCollection.AddAspectSupport(aspectConfigurationProviderMock.Object);
            aspectConfigurationProviderMock.Verify(x => x.GetTypeAspectConfiguration(typeof(ITestInterface), typeof(MyTestInterface)), Times.Once);
            serviceCollection.BuildServiceProvider().GetService<ITestInterface>().Should().NotBeNull();
        }
    }
}