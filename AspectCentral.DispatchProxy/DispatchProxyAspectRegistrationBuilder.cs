using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using JamesConsulting;
using Microsoft.Extensions.DependencyInjection;

namespace AspectCentral.DispatchProxy;

/// <summary>
/// Implementation of <see cref="IAspectRegistrationBuilder" /> that uses <see cref="System.Reflection.DispatchProxy" />
/// to wrap services with cross-cutting concerns.
/// </summary>
public class DispatchProxyAspectRegistrationBuilder(
    IServiceCollection services,
    IAspectConfigurationProvider aspectConfigurationProvider)
    : AspectRegistrationBuilder(services, aspectConfigurationProvider)
{
    /// <summary>
    /// Cached <see cref="MethodInfo" /> for the private generic factory method used to build
    /// proxy instances after aspects have been registered fluently.
    /// </summary>
    /// <remarks>
    /// <c>BindingFlags.NonPublic</c> is intentional: <c>CreateFactory&lt;TService&gt;</c> is a private
    /// generic invoked by <see cref="InvokeCreateFactory" /> after closing it over the runtime
    /// <c>TService</c>. It is not API.
    /// </remarks>
    [SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
        Justification = "Intentional: CreateFactory<TService> is a private generic factory closed over runtime TService in InvokeCreateFactory; it is not API.")]
    private static readonly MethodInfo CreateFactoryMethodInfo =
        typeof(DispatchProxyAspectRegistrationBuilder).GetMethod(nameof(CreateFactory),
            BindingFlags.Static | BindingFlags.NonPublic)!;

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
    /// Builds and executes the aspect chain factory for a service descriptor registered with either
    /// an implementation type or an implementation factory.
    /// </summary>
    /// <param name="serviceProvider">
    /// Service provider resolving the proxied service instance.
    /// </param>
    /// <param name="aspectConfiguration">
    /// Aspect configuration associated with the service descriptor being resolved.
    /// </param>
    /// <typeparam name="TService">The interface service type being resolved.</typeparam>
    /// <returns>
    /// A proxied service instance with all configured aspects applied.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown by dependency injection when the implementation or any configured aspect factory
    /// cannot be resolved.
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
            instance = (TService)aspectConfiguration.ServiceDescriptor.ImplementationFactory!(serviceProvider);
            implementationType = instance.GetObjectType();
        }

        Func<IServiceProvider, TService> factory = f =>
            aspectConfiguration.ServiceDescriptor.ImplementationType != null
                ? (TService)f.GetRequiredService(aspectConfiguration.ServiceDescriptor.ImplementationType)
                : instance!;

        foreach (var aspectType in aspectConfiguration.GetAspects().Select(a => a.AspectType))
        {
            var temp = factory;
            factory = f =>
            {
                var interceptorFactory = (IAspectFactory)f.GetRequiredService(aspectType);
                return interceptorFactory.Create(temp(f), implementationType!);
            };
        }

        return factory(serviceProvider);
    }

#pragma warning restore S1144 // Unused private types or members should be removed

    /// <inheritdoc />
    public override object InvokeCreateFactory(IServiceProvider serviceProvider,
        AspectConfiguration aspectConfiguration)
    {
        var mi = CreateFactoryMethodInfo.MakeGenericMethod(aspectConfiguration.ServiceDescriptor.ServiceType);
        return mi.Invoke(null, [serviceProvider, aspectConfiguration])!;
    }
}
