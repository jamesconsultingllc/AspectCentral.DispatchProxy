using System;
using System.Reflection;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using JamesConsulting;
using Microsoft.Extensions.DependencyInjection;

namespace AspectCentral.DispatchProxy
{
    /// <summary>
    ///     The aspect registration builder.
    /// </summary>
    public class DispatchProxyAspectRegistrationBuilder : AspectRegistrationBuilder
    {
        /// <summary>
        ///     The create factory method info.
        /// </summary>
        private static readonly MethodInfo CreateFactoryMethodInfo =
            typeof(DispatchProxyAspectRegistrationBuilder).GetMethod("CreateFactory", BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        ///     Initializes a new instance of the <see cref="DispatchProxyAspectRegistrationBuilder" /> class.
        /// </summary>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <param name="aspectConfigurationProvider">
        ///     The aspect configuration provider.
        /// </param>
        public DispatchProxyAspectRegistrationBuilder(IServiceCollection services,
            IAspectConfigurationProvider aspectConfigurationProvider) : base(services, aspectConfigurationProvider)
        {
            
        }

        /// <inheritdoc />
        public override void ValidateAddAspect(Type aspectType)
        {
            base.ValidateAddAspect(aspectType);
            
            if (!Constants.IAspectFactoryType.IsAssignableFrom(aspectType))
                throw new ArgumentException(
                    $"The {nameof(aspectType)} must be a concrete class that implements the {Constants.IAspectFactoryType} interface",
                    nameof(aspectType));
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
                var interceptorFactory = (IAspectFactory) serviceProvider.GetService(aspect.AspectType);
                factory = f => interceptorFactory.Create(temp(serviceProvider), implementationType);
            }

            return factory(serviceProvider);
        }

#pragma warning restore S1144 // Unused private types or members should be removed

        /// <inheritdoc />
        public override object InvokeCreateFactory(IServiceProvider serviceProvider, AspectConfiguration aspectConfiguration)
        {
            var mi = CreateFactoryMethodInfo.MakeGenericMethod(aspectConfiguration.ServiceDescriptor.ServiceType);
            return mi.Invoke(null, new object[] {serviceProvider, aspectConfiguration});
        }
    }
}