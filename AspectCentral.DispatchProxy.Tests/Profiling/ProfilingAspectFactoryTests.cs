using System;
using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy.Profiling;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AspectCentral.DispatchProxy.Tests.Profiling
{
    public class ProfilingAspectFactoryTests
    {
        private readonly ProfilingAspectFactory instance;

        [Fact]
        public void NullLoggerFactoryThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ProfilingAspectFactory(null, null));
        }

        [Fact]
        public void NullAspectConfigurationProviderThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ProfilingAspectFactory(new NullLoggerFactory(), null));
        }

        [Fact]
        public void ProfilingAspectFactoryConstructorSucceeds()
        {
            new ProfilingAspectFactory(new NullLoggerFactory(), new InMemoryAspectConfigurationProvider()).Should().NotBeNull();
        }

        public ProfilingAspectFactoryTests()
        {
            instance = new ProfilingAspectFactory(new NullLoggerFactory(), new InMemoryAspectConfigurationProvider());
        }
        
        [Fact]
        public void CreateNullInstanceThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => instance.Create(default(MyTestInterface), typeof(MyTestInterface)));
        }
        
        [Fact]
        public void CreateNullTypeThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => instance.Create(new MyTestInterface(), null));
        }
        
        [Fact]
        public void CreatedObjectShouldNotBeNull()
        {
            instance.Create<ITestInterface>(new MyTestInterface(), typeof(MyTestInterface)).Should().NotBeNull();
        }
    }
}