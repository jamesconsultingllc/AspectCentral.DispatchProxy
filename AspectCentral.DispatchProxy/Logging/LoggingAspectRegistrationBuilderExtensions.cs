// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingAspectRegistrationBuilderExtensions.cs" company="James Consulting LLC">
//   
// </copyright>
// <summary>
//   The logging aspect registration builder extensions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using AspectCentral.Abstractions;

namespace AspectCentral.DispatchProxy.Logging
{
    /// <summary>
    ///     The logging aspect registration builder extensions.
    /// </summary>
    public static class LoggingAspectRegistrationBuilderExtensions
    {
        /// <summary>
        /// The with logging.
        /// </summary>
        /// <param name="aspectRegistrationBuilder">
        /// The aspect registration builder.
        /// </param>
        /// <param name="methodsToIntercept">
        /// The methods To Intercept.
        /// </param>
        /// <returns>
        /// The <see cref="IAspectRegistrationBuilder"/>.
        /// </returns>
        public static IAspectRegistrationBuilder AddLoggingAspect(this IAspectRegistrationBuilder aspectRegistrationBuilder, params MethodInfo[] methodsToIntercept)
        {
            if (aspectRegistrationBuilder == null) throw new ArgumentNullException(nameof(aspectRegistrationBuilder));

            aspectRegistrationBuilder.AddAspect(LoggingAspectFactory.LoggingAspectFactoryType, methodsToIntercept: methodsToIntercept);
            return aspectRegistrationBuilder;
        }
    }
}