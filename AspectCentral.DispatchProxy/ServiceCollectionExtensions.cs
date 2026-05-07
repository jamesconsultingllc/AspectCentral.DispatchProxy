//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="ServiceCollectionExtensions.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
//    Provides extension methods for <see cref="IServiceCollection"/> to enable Aspect-Oriented Programming (AOP) support.
//    These methods allow you to easily register aspects and wrap services with cross-cutting concerns.
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspectCentral.DispatchProxy;
/// <summary>
///     Extension methods on <see cref="IServiceCollection"/> that enable Aspect-Oriented Programming
///     support backed by <see cref="System.Reflection.DispatchProxy"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Cached <see cref="MethodInfo"/> for the private generic <c>CreateFactory&lt;TService&gt;</c>
    ///     used to construct the aspect chain at service-resolution time.
    /// </summary>
private static readonly MethodInfo CreateFactoryMethodInfo =
    typeof(ServiceCollectionExtensions).GetMethod("CreateFactory",
        BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    ///     Enables aspect-oriented programming on the supplied <see cref="IServiceCollection"/> using
    ///     an in-memory configuration provider, and returns a fluent builder for declaring which
    ///     aspects wrap which services.
    /// </summary>
    /// <param name="serviceCollection">The service collection to configure.</param>
    /// <returns>
    ///     A <see cref="IAspectRegistrationBuilder"/> implementation backed by
    ///     <see cref="DispatchProxyAspectRegistrationBuilder"/>. Chain
    ///     <c>.AddLoggingAspect()</c>, <c>.AddProfilingAspect()</c>, or
    ///     <see cref="IAspectRegistrationBuilderExtensions.AddAspectViaFactory{T}"/> against the result.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Side effects performed on the service collection:
    ///         <list type="bullet">
    ///             <item><description>Registers a singleton <see cref="InMemoryAspectConfigurationProvider"/> as <see cref="IAspectConfigurationProvider"/> if one is not already registered.</description></item>
    ///             <item><description>Reflectively scans every assembly currently loaded in <see cref="AppDomain.CurrentDomain"/> for concrete <see cref="IAspectFactory"/> implementations and registers each as a singleton against itself (idempotent via <c>TryAddSingleton</c>).</description></item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Aspect factories that live in plug-in or lazy-loaded assemblies that are not yet loaded at
    ///         the moment this method runs will <strong>not</strong> be discovered. For those scenarios
    ///         use the <see cref="AddAspectSupport(IServiceCollection, System.Reflection.Assembly[])"/>
    ///         overload to supply the assemblies explicitly.
    ///     </para>
    /// </remarks>
    public static IAspectRegistrationBuilder AddAspectSupport(this IServiceCollection serviceCollection)
    {
        if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
        var aspectConfigurationProvider = GetOrAddInMemoryProvider(serviceCollection);
        return new DispatchProxyAspectRegistrationBuilder(serviceCollection.RegisterAspectFactories(),
            aspectConfigurationProvider);
    }

    /// <summary>
    ///     Enables aspect-oriented programming using the caller-supplied set of assemblies to scan for
    ///     <see cref="IAspectFactory"/> implementations. Use this overload in plug-in / module-loader
    ///     scenarios where factories live in assemblies that may not yet be loaded into the
    ///     <see cref="AppDomain"/> when registration runs.
    /// </summary>
    /// <param name="serviceCollection">The service collection to configure.</param>
    /// <param name="assembliesToScan">
    ///     Explicit list of assemblies to scan for <see cref="IAspectFactory"/> implementations. May be
    ///     empty; in that case no factories are auto-registered and the caller is expected to register
    ///     them manually before chaining aspect declarations.
    /// </param>
    /// <returns>A fluent <see cref="IAspectRegistrationBuilder"/> for declaring aspect-to-service mappings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceCollection"/> or <paramref name="assembliesToScan"/> is <see langword="null"/>.</exception>
    public static IAspectRegistrationBuilder AddAspectSupport(this IServiceCollection serviceCollection,
        params System.Reflection.Assembly[] assembliesToScan)
    {
        if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
        if (assembliesToScan == null) throw new ArgumentNullException(nameof(assembliesToScan));

        var aspectConfigurationProvider = GetOrAddInMemoryProvider(serviceCollection);
        return new DispatchProxyAspectRegistrationBuilder(
            serviceCollection.RegisterAspectFactories(assembliesToScan),
            aspectConfigurationProvider);
    }

    /// <summary>
    ///     Enables aspect-oriented programming on the supplied <see cref="IServiceCollection"/> using
    ///     a caller-supplied <see cref="IAspectConfigurationProvider"/>, then immediately rewrites the
    ///     collection so that every interface descriptor whose implementation has a configured aspect
    ///     resolves to a proxy at service-resolution time.
    /// </summary>
    /// <param name="serviceCollection">The service collection to configure. Must not be <see langword="null"/>.</param>
    /// <param name="aspectConfigurationProvider">
    ///     A pre-populated configuration provider describing which aspects apply to which
    ///     <c>(serviceType, implementationType)</c> pair. Must not be <see langword="null"/>.
    /// </param>
    /// <returns>The same <paramref name="serviceCollection"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="serviceCollection"/> or <paramref name="aspectConfigurationProvider"/>
    ///     is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///     Use this overload when you have constructed an <see cref="AspectConfiguration"/> ahead of
    ///     time (for example, deserialized from configuration). For the fluent registration flow,
    ///     prefer <see cref="AddAspectSupport(IServiceCollection)"/>.
    /// </remarks>
public static IServiceCollection AddAspectSupport(this IServiceCollection serviceCollection,
    IAspectConfigurationProvider aspectConfigurationProvider)
{
    if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
    if (aspectConfigurationProvider == null) throw new ArgumentNullException(nameof(aspectConfigurationProvider));

    // Detect provider-mismatch: if a different IAspectConfigurationProvider instance is already
    // registered, ConfigureAspects would rewrite descriptors using the supplied instance while DI
    // hands aspect factories the pre-existing one at runtime, leading to silently incorrect interception.
    var existing = serviceCollection.FirstOrDefault(d =>
        d.ServiceType == typeof(IAspectConfigurationProvider));
    if (existing is not null)
    {
        if (!ReferenceEquals(existing.ImplementationInstance, aspectConfigurationProvider))
        {
            throw new InvalidOperationException(
                "An IAspectConfigurationProvider is already registered in the service collection " +
                "with a different instance than the one supplied to AddAspectSupport. Remove the " +
                "prior registration or call the parameterless AddAspectSupport(IServiceCollection) " +
                "fluent overload instead.");
        }
    }
    else
    {
        serviceCollection.AddSingleton(aspectConfigurationProvider);
    }

    return serviceCollection.RegisterAspectFactories().ConfigureAspects(aspectConfigurationProvider);
}

/// <summary>
///     Rewrites interface→implementation descriptors in the service collection so that resolving the
///     interface yields a <see cref="System.Reflection.DispatchProxy" /> wrapping the concrete
///     implementation with the configured aspects.
/// </summary>
/// <param name="serviceCollection">The service collection to scan and rewrite.</param>
/// <param name="aspectConfigurationProvider">Provider supplying aspect configurations per type pair.</param>
/// <returns>The same <paramref name="serviceCollection"/> for chaining.</returns>
/// <remarks>
///     <para>
///         Only descriptors that satisfy ALL of the following are considered:
///         <list type="bullet">
///             <item><description><c>ServiceType</c> is an interface and is closed (no open generic parameters).</description></item>
///             <item><description>Descriptor is non-keyed.</description></item>
///             <item><description><c>ImplementationType</c> is non-null (i.e. not an instance or factory descriptor).</description></item>
///         </list>
///     </para>
///     <para>
///         Open generics, keyed services, <c>ImplementationInstance</c> registrations, and
///         <c>ImplementationFactory</c> registrations are intentionally left untouched in this rewrite
///         path because proxying them safely requires preserving DI ownership/disposal semantics that
///         a static descriptor swap cannot guarantee. Consumers needing aspects on those registration
///         shapes should use the fluent builder
///         (<see cref="AddAspectSupport(IServiceCollection)"/> + <c>AddAspectViaFactory</c>), whose
///         <see cref="DispatchProxyAspectRegistrationBuilder"/> handles factory descriptors explicitly.
///     </para>
///     <para>
///         The loop captures the original descriptor count before iterating so that descriptors
///         appended during rewrite (the concrete implementation type registration) are not themselves
///         re-evaluated.
///     </para>
/// </remarks>
private static IServiceCollection ConfigureAspects(this IServiceCollection serviceCollection,
    IAspectConfigurationProvider aspectConfigurationProvider)
{
    var originalCount = serviceCollection.Count;
    for (var index = 0; index < originalCount; index++)
    {
        var service = serviceCollection[index];

        if (!service.ServiceType.IsInterface) continue;
        if (service.ServiceType.ContainsGenericParameters) continue;
        if (service.IsKeyedService) continue;
        if (service.ImplementationType is null) continue;

        var aspectConfiguration =
            aspectConfigurationProvider.GetTypeAspectConfiguration(service.ServiceType,
                service.ImplementationType);

        if (aspectConfiguration is null) continue;

        // The proxy factory below resolves the concrete implementation through DI, so the
        // implementation type must be registered. If a registration with a *different* lifetime
        // already exists, TryAdd would silently leave it in place and the proxied interface
        // would resolve a concrete instance with the wrong lifetime (e.g. interface declared
        // Transient but a pre-existing Singleton registration of the impl wins). Detect this
        // and throw with clear remediation rather than corrupt DI semantics.
        var implType = service.ImplementationType;
        var existingConcrete = serviceCollection.FirstOrDefault(d =>
            d.ServiceType == implType && !d.IsKeyedService);
        if (existingConcrete is null)
        {
            serviceCollection.Add(ServiceDescriptor.Describe(implType, implType, service.Lifetime));
        }
        else if (existingConcrete.Lifetime != service.Lifetime)
        {
            throw new InvalidOperationException(
                $"Cannot enable aspects on {service.ServiceType.FullName} -> {implType.FullName}: " +
                $"the implementation type is already registered with lifetime " +
                $"{existingConcrete.Lifetime} but the interface descriptor uses {service.Lifetime}. " +
                $"Remove the conflicting registration or align the lifetimes before calling AddAspectSupport.");
        }

        serviceCollection[index] = new ServiceDescriptor(service.ServiceType,
            serviceProvider => InvokeCreateFactory(serviceProvider, aspectConfiguration), service.Lifetime);
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
    AspectConfiguration aspectConfiguration) where TService : class
{
    Func<IServiceProvider, TService> factory = f =>
        (TService) f.GetRequiredService(aspectConfiguration.ServiceDescriptor.ImplementationType!);

    foreach (var aspect in aspectConfiguration.GetAspects())
    {
        var temp = factory;
        var aspectType = aspect.AspectType;
        factory = f =>
        {
            var interceptorFactory = (IAspectFactory) f.GetRequiredService(aspectType);
            return interceptorFactory.Create(temp(f),
                aspectConfiguration.ServiceDescriptor.ImplementationType!);
        };
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
    return mi.Invoke(null, [serviceProvider, aspectConfiguration])!;
}

    /// <summary>
    ///     Scans the assemblies currently loaded in <see cref="AppDomain.CurrentDomain"/> for
    ///     concrete <see cref="IAspectFactory"/> implementations and registers each as a singleton.
    /// </summary>
    private static IServiceCollection RegisterAspectFactories(this IServiceCollection serviceCollection)
        => serviceCollection.RegisterAspectFactories(AppDomain.CurrentDomain.GetAssemblies());

    /// <summary>
    ///     Returns the already-registered singleton <see cref="IAspectConfigurationProvider"/>
    ///     instance from <paramref name="serviceCollection"/> if one exists, otherwise registers a
    ///     new <see cref="InMemoryAspectConfigurationProvider"/> as a singleton instance and returns
    ///     it. Guarantees that the builder returned by <c>AddAspectSupport</c> writes into the same
    ///     provider DI will resolve at runtime — closing the prior bug where the builder mutated a
    ///     fresh provider while DI kept an existing one.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when an <see cref="IAspectConfigurationProvider"/> is already registered via a type
    ///     or factory descriptor (no resolved singleton instance available). Callers in that case
    ///     should use <see cref="AddAspectSupport(IServiceCollection, IAspectConfigurationProvider)"/>
    ///     and pass the provider explicitly.
    /// </exception>
    private static IAspectConfigurationProvider GetOrAddInMemoryProvider(IServiceCollection serviceCollection)
    {
        for (var i = 0; i < serviceCollection.Count; i++)
        {
            var d = serviceCollection[i];
            if (d.ServiceType != typeof(IAspectConfigurationProvider)) continue;
            if (d.ImplementationInstance is IAspectConfigurationProvider existing) return existing;
            throw new InvalidOperationException(
                "An IAspectConfigurationProvider is already registered without a singleton instance; " +
                "the fluent AddAspectSupport overload cannot reuse a type- or factory-registered provider. " +
                "Use AddAspectSupport(IServiceCollection, IAspectConfigurationProvider) instead and pass the provider explicitly.");
        }

        var provider = new InMemoryAspectConfigurationProvider();
        serviceCollection.AddSingleton<IAspectConfigurationProvider>(provider);
        return provider;
    }

    /// <summary>
    ///     Scans the supplied assemblies for concrete <see cref="IAspectFactory"/> implementations and
    ///     registers each as a singleton (idempotent via <c>TryAddSingleton</c>). Assemblies that fail
    ///     <see cref="System.Reflection.Assembly.GetTypes"/> with a <see cref="ReflectionTypeLoadException"/>
    ///     contribute whatever non-null types the runtime managed to load.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static IServiceCollection RegisterAspectFactories(this IServiceCollection serviceCollection,
        System.Reflection.Assembly[] assembliesToScan)
    {
        var types = assembliesToScan
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.Where(t => t != null)!;
                }
            })
            .Where(type => type != null && !type.IsAbstract && !type.IsInterface &&
                           Constants.IAspectFactoryType.IsAssignableFrom(type));

        foreach (var type in types)
        {
            if (type != null)
            {
                serviceCollection.TryAddSingleton(type);
            }
        }

        return serviceCollection;
    }
}


