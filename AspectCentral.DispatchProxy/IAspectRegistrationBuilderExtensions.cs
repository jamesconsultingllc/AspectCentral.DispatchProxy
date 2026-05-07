//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="IAspectRegistrationBuilderExtensions.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System.Reflection;
using AspectCentral.Abstractions;

namespace AspectCentral.DispatchProxy
{
    /// <summary>
    ///     Generic, factory-typed extensions over <see cref="IAspectRegistrationBuilder"/> that
    ///     register an aspect by its <see cref="IAspectFactory"/> implementation type.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IAspectRegistrationBuilderExtensions
    {
        /// <summary>
        ///     Registers the aspect produced by the factory <typeparamref name="T"/> against every
        ///     interface-based service descriptor currently known to the builder.
        /// </summary>
        /// <param name="aspectRegistrationBuilder">
        ///     The fluent aspect registration builder. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="sortOrder">
        ///     Optional ordering value within the aspect chain for the affected services. Lower values run
        ///     closer to the caller (i.e. wrap later aspects). When <see langword="null"/>, the aspect is
        ///     appended after any previously registered aspects.
        /// </param>
        /// <param name="methodsToIntercept">
        ///     Optional method-level filter. When non-empty, only the listed <see cref="MethodInfo"/>
        ///     entries trigger the aspect's <c>PreInvoke</c>/<c>PostInvoke</c> hooks; other methods are
        ///     dispatched to the underlying service without interception. When empty, every method is
        ///     intercepted.
        /// </param>
        /// <typeparam name="T">
        ///     The <see cref="IAspectFactory"/> implementation that produces the aspect proxy. The
        ///     factory must be registered in DI; it is registered automatically by
        ///     <see cref="ServiceCollectionExtensions.AddAspectSupport(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>.
        /// </typeparam>
        /// <returns>The same <see cref="IAspectRegistrationBuilder"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="aspectRegistrationBuilder"/> is <see langword="null"/>.</exception>
        public static IAspectRegistrationBuilder AddAspectViaFactory<T>(this IAspectRegistrationBuilder aspectRegistrationBuilder, int? sortOrder = null, params MethodInfo[] methodsToIntercept)
            where T : IAspectFactory
        {
            if (aspectRegistrationBuilder == null) throw new ArgumentNullException(nameof(aspectRegistrationBuilder));
            aspectRegistrationBuilder.AddAspect(typeof(T), sortOrder, methodsToIntercept);
            return aspectRegistrationBuilder;
        }
    }
}