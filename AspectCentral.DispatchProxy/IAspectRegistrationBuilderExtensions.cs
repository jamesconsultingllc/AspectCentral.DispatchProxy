//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="IAspectRegistrationBuilderExtensions.cs" company="James Consulting LLC">
//    Copyright (c) 2019 All Rights Reserved
//  </copyright>
//  <author>Rudy James</author>
//  <summary>
// 
//  </summary>
//  ----------------------------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using AspectCentral.Abstractions;

namespace AspectCentral.DispatchProxy
{
    // ReSharper disable once InconsistentNaming
    public static class IAspectRegistrationBuilderExtensions
    {
        /// <summary>
        ///     The add aspect.
        /// </summary>
        /// <param name="aspectRegistrationBuilder">
        ///     The aspect registration builder.
        /// </param>
        /// <param name="sortOrder">
        ///     The sort order.
        /// </param>
        /// <param name="methodsToIntercept">
        ///     The methods to intercept.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IAspectRegistrationBuilder" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static IAspectRegistrationBuilder AddAspectViaFactory<T>(this IAspectRegistrationBuilder aspectRegistrationBuilder, int? sortOrder = null, params MethodInfo[] methodsToIntercept)
            where T : IAspectFactory
        {
            if (aspectRegistrationBuilder == null) throw new ArgumentNullException(nameof(aspectRegistrationBuilder));
            aspectRegistrationBuilder.AddAspect(typeof(T), sortOrder, methodsToIntercept);
            return aspectRegistrationBuilder;
        }
    }
}