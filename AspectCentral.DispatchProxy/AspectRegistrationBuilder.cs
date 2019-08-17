using System;
using System.Linq;
using System.Reflection;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using JamesConsulting;
using JamesConsulting.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspectCentral.DispatchProxy
{
    /// <summary>
    ///     The aspect registration builder.
    /// </summary>
    public class AspectRegistrationBuilder : IAspectRegistrationBuilder
    {
        /// <summary>
        ///     The create factory method info.
        /// </summary>
        private static readonly MethodInfo CreateFactoryMethodInfo =
            typeof(AspectRegistrationBuilder).GetMethod("CreateFactory", BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        ///     Initializes a new instance of the <see cref="AspectRegistrationBuilder" /> class.
        /// </summary>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <param name="aspectConfigurationProvider">
        ///     The aspect configuration provider.
        /// </param>
        public AspectRegistrationBuilder(IServiceCollection services,
            IAspectConfigurationProvider aspectConfigurationProvider)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            AspectConfigurationProvider = aspectConfigurationProvider ??
                                          throw new ArgumentNullException(nameof(aspectConfigurationProvider));
        }

        /// <inheritdoc />
        public IAspectConfigurationProvider AspectConfigurationProvider { get; }

        /// <inheritdoc />
        public IServiceCollection Services { get; }

        /// <inheritdoc />
        public IAspectRegistrationBuilder AddAspect(Type aspectFactory, int? sortOrder = null,
            params MethodInfo[] methodsToIntercept)
        {
            if (aspectFactory == null) throw new ArgumentNullException(nameof(aspectFactory));
            if (!Constants.IAspectFactoryType.IsAssignableFrom(aspectFactory))
                throw new ArgumentException(
                    $"The {nameof(aspectFactory)} must be a concrete class that implements the {Constants.IAspectFactoryType} interface",
                    nameof(aspectFactory));

            if (AspectConfigurationProvider.ConfigurationEntries.Count == 0)
                throw new InvalidOperationException("A service must be registered to apply an aspect to.");
            AspectConfigurationProvider.ConfigurationEntries.Last()
                .AddEntry(aspectFactory, sortOrder, methodsToIntercept);
            return this;
        }

        /// <inheritdoc />
        public IAspectRegistrationBuilder AddService(Type service, Type implementation, ServiceLifetime serviceLifetime)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            if (!implementation.IsConcreteClass() || !service.IsAssignableFrom(implementation))
                throw new ArgumentException(
                    $"The {nameof(implementation)} ({implementation.FullName}) must be a concrete class that implements the {nameof(service)} ({service.Name})");

            var aspectConfiguration =
                new AspectConfiguration(new ServiceDescriptor(service, implementation, serviceLifetime));
            Services.TryAdd(new ServiceDescriptor(implementation, implementation, serviceLifetime));
            Services.Add(new ServiceDescriptor(service,
                serviceProvider => InvokeCreateFactory(serviceProvider, aspectConfiguration), serviceLifetime));
            var serviceDescriptor = new ServiceDescriptor(service, implementation, serviceLifetime);
            AspectConfigurationProvider.AddEntry(aspectConfiguration);
            Services.TryAdd(serviceDescriptor);
            return this;
        }

        /// <inheritdoc />
        public IAspectRegistrationBuilder AddService(Type service, Func<IServiceProvider, object> factory,
            ServiceLifetime serviceLifetime)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var aspectConfiguration = new AspectConfiguration(new ServiceDescriptor(service, factory, serviceLifetime));
            AspectConfigurationProvider.AddEntry(aspectConfiguration);
            Services.Add(new ServiceDescriptor(service,
                serviceProvider => InvokeCreateFactory(serviceProvider, aspectConfiguration), serviceLifetime));
            return this;
        }

        /// <summary>
        ///     The create factory.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The service provider.
        /// </param>
        /// <param name="aspectConfiguration">
        ///     The service descriptor.
        /// </param>
        /// <returns>
        ///     The <see cref="object" />.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>

        // ReSharper disable once UnusedMember.Local
#pragma warning disable S1144 // Unused private types or members should be removed
        private static object CreateFactory<TService>(IServiceProvider serviceProvider,
            AspectConfiguration aspectConfiguration)
        {
            var implementationType = aspectConfiguration.ServiceDescriptor.ImplementationType;
            var instance = default(TService);

            if (implementationType == null)
            {
                instance = (TService) aspectConfiguration.ServiceDescriptor.ImplementationFactory(serviceProvider);
                implementationType = instance.GetObjectType();
            }

            Func<IServiceProvider, TService> factory = f =>
                aspectConfiguration.ServiceDescriptor.ImplementationType != null
                    ? (TService) f.GetService(aspectConfiguration.ServiceDescriptor.ImplementationType)
                    : instance;

            foreach (var aspect in aspectConfiguration.GetAspects())
            {
                var temp = factory;
                var interceptorFactory = (IAspectFactory) serviceProvider.GetService(aspect.AspectFactoryType);
                factory = f => interceptorFactory.Create(temp(serviceProvider), implementationType);
            }

            return factory(serviceProvider);
        }

#pragma warning restore S1144 // Unused private types or members should be removed

        /// <summary>
        ///     The invoke create factory.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The service provider.
        /// </param>
        /// <param name="aspectConfiguration">
        ///     The aspect configuration.
        /// </param>
        /// <returns>
        ///     The <see cref="object" />.
        /// </returns>
        private static object InvokeCreateFactory(IServiceProvider serviceProvider,
            AspectConfiguration aspectConfiguration)
        {
            var mi = CreateFactoryMethodInfo.MakeGenericMethod(aspectConfiguration.ServiceDescriptor.ServiceType);
            return mi.Invoke(null, new object[] {serviceProvider, aspectConfiguration});
        }
    }
}