using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using JamesConsulting;
using Microsoft.Extensions.DependencyInjection;

namespace AspectCentral.DispatchProxy;

/// <summary>
///     Implementation of <see cref="IAspectRegistrationBuilder"/> that uses <see cref="System.Reflection.DispatchProxy"/>
///     to wrap services with cross-cutting concerns.
/// </summary>
public class DispatchProxyAspectRegistrationBuilder(IServiceCollection services,
    IAspectConfigurationProvider aspectConfigurationProvider) : AspectRegistrationBuilder(services, aspectConfigurationProvider)
{
    /// <summary>
    ///     The create factory method info.
    /// </summary>
    private static readonly MethodInfo CreateFactoryMethodInfo =
        typeof(DispatchProxyAspectRegistrationBuilder).GetMethod("CreateFactory", BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <inheritdoc />
    protected override void ValidateAddAspect(Type aspectType)
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
    [ExcludeFromCodeCoverage]
    private static object CreateFactory<TService>(IServiceProvider serviceProvider,
        AspectConfiguration aspectConfiguration) where TService : class
    {
        var implementationType = aspectConfiguration.ServiceDescriptor.ImplementationType;
        var instance = default(TService);

        if (implementationType == null)
        {
            instance = (TService) aspectConfiguration.ServiceDescriptor.ImplementationFactory!(serviceProvider);
            implementationType = instance.GetObjectType();
        }

        Func<IServiceProvider, TService> factory = f =>
            aspectConfiguration.ServiceDescriptor.ImplementationType != null
                ? (TService) f.GetRequiredService(aspectConfiguration.ServiceDescriptor.ImplementationType)
                : instance!;

        foreach (var aspect in aspectConfiguration.GetAspects())
        {
            var temp = factory;
            var aspectType = aspect.AspectType;
            factory = f =>
            {
                var interceptorFactory = (IAspectFactory) f.GetRequiredService(aspectType);
                return interceptorFactory.Create(temp(f), implementationType!);
            };
        }

        return factory(serviceProvider);
    }

#pragma warning restore S1144 // Unused private types or members should be removed

    /// <inheritdoc />
    public override object InvokeCreateFactory(IServiceProvider serviceProvider, AspectConfiguration aspectConfiguration)
    {
        var mi = CreateFactoryMethodInfo.MakeGenericMethod(aspectConfiguration.ServiceDescriptor.ServiceType);
        return mi.Invoke(null, [serviceProvider, aspectConfiguration])!;
    }
}