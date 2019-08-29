//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="ServiceCollectionExtensions.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspectCentral.DispatchProxy
{
    /// <summary>
    ///     The aspects service collection extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     The create factory method info.
        /// </summary>
        private static readonly MethodInfo CreateFactoryMethodInfo =
            typeof(ServiceCollectionExtensions).GetMethod("CreateFactory",
                BindingFlags.Static | BindingFlags.NonPublic);

        public static IAspectRegistrationBuilder AddAspectSupport(this IServiceCollection serviceCollection)
        {
            var aspectConfigurationProvider = new InMemoryAspectConfigurationProvider();
            serviceCollection.TryAddSingleton<IAspectConfigurationProvider>(aspectConfigurationProvider);
            return new DispatchProxyAspectRegistrationBuilder(serviceCollection.RegisterAspectFactories(),
                aspectConfigurationProvider);
        }
        /// <summary>
        ///     The register aspect support.
        /// </summary>
        /// <param name="serviceCollection">
        ///     The service collection.
        /// </param>
        /// <param name="aspectConfigurationProvider">
        ///     The aspect Configuration Provider.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static IServiceCollection AddAspectSupport(this IServiceCollection serviceCollection,
            IAspectConfigurationProvider aspectConfigurationProvider)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (aspectConfigurationProvider == null)
                throw new ArgumentNullException(nameof(aspectConfigurationProvider));
            serviceCollection.TryAddSingleton(aspectConfigurationProvider);
            return serviceCollection.RegisterAspectFactories().ConfigureAspects(aspectConfigurationProvider);
        }

        /// <summary>
        ///     The configure aspects.
        /// </summary>
        /// <param name="serviceCollection">
        ///     The service collection.
        /// </param>
        /// <param name="aspectConfigurationProvider">
        ///     The aspect configuration provider.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" />.
        /// </returns>
        private static IServiceCollection ConfigureAspects(this IServiceCollection serviceCollection,
            IAspectConfigurationProvider aspectConfigurationProvider)
        {
            for (var index = 0; index < serviceCollection.Count; index++)
            {
                var service = serviceCollection[index];

                if (service.ServiceType.IsInterface && service.ImplementationType != null)
                {
                    var aspectConfiguration =
                        aspectConfigurationProvider.GetTypeAspectConfiguration(service.ServiceType,
                            service.ImplementationType);

                    if (aspectConfiguration == null) continue;

                    serviceCollection.TryAdd(ServiceDescriptor.Describe(service.ImplementationType, service.ImplementationType, service.Lifetime));
                    serviceCollection[index] = new ServiceDescriptor(service.ServiceType,
                        serviceProvider => InvokeCreateFactory(serviceProvider, aspectConfiguration), service.Lifetime);
                }
            }

            return serviceCollection;
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
            Func<IServiceProvider, TService> factory = f =>
                (TService) f.GetService(aspectConfiguration.ServiceDescriptor.ImplementationType);

            foreach (var aspect in aspectConfiguration.GetAspects())
            {
                var temp = factory;
                var interceptorFactory = (IAspectFactory) serviceProvider.GetService(aspect.AspectType);
                factory = f => interceptorFactory.Create(temp(serviceProvider),
                    aspectConfiguration.ServiceDescriptor.ImplementationType);
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

        /// <summary>
        ///     The register aspect factories.
        /// </summary>
        /// <param name="serviceCollection">
        ///     The service collection.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" />.
        /// </returns>
        private static IServiceCollection RegisterAspectFactories(this IServiceCollection serviceCollection)
        {
            var types = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where !type.IsAbstract && !type.IsInterface &&
                      Constants.IAspectFactoryType.IsAssignableFrom(type)
                select type;

            foreach (var type in types) serviceCollection.TryAddSingleton(type);

            return serviceCollection;
        }
    }
}