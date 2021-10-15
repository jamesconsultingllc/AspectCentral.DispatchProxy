//  ----------------------------------------------------------------------------------------------------------------------
//  <copyright file="BaseAspect.cs" company="James Consulting LLC">
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
using System.Threading.Tasks;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using JamesConsulting.Reflection;
using Microsoft.Extensions.Logging;
using MethodTypeOptions = AspectCentral.Abstractions.MethodTypeOptions;

namespace AspectCentral.DispatchProxy
{
    /// <summary>
    ///     The base aspect.
    /// </summary>
    /// <typeparam name="T">
    /// </typeparam>
    public abstract class BaseAspect<T> : System.Reflection.DispatchProxy where T : class?

    {
    /// <summary>
    ///     The process function method info.
    /// </summary>
    private static readonly MethodInfo ProcessFunctionMethodInfo =
        typeof(BaseAspect<T>).GetMethod("ProcessFunctionAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    ///     Gets or sets the factory type
    /// </summary>
    protected Type? FactoryType { get; set; }

    /// <summary>
    ///     Gets or sets the aspect configuration provider
    /// </summary>
    protected IAspectConfigurationProvider? AspectConfigurationProvider { get; set; }

    /// <summary>
    ///     Gets or sets the instance.
    /// </summary>
    protected T? Instance { get; set; }

    /// <summary>
    ///     Gets the logger.
    /// </summary>
    protected ILogger? Logger { get; set; }

    /// <summary>
    ///     Gets or sets the object type.
    /// </summary>
    protected Type? ObjectType { get; set; }

    /// <summary>
    ///     The generate aspect context.
    /// </summary>
    /// <param name="targetMethod">
    ///     The target method.
    /// </param>
    /// <param name="args">
    ///     The args.
    /// </param>
    /// <returns>
    ///     The <see cref="AspectContext" />.
    /// </returns>
    public virtual AspectContext GenerateAspectContext(MethodInfo targetMethod, object[] args)
    {
        return new AspectContext(targetMethod, args)
        {
            InvocationString = GenerateMethodNameWithArguments(targetMethod, args, out var implementationMethod),
            InstanceMethod = implementationMethod
        };
    }

    /// <summary>
    ///     The generate method name with arguments.
    /// </summary>
    /// <param name="targetMethod">
    ///     The target method.
    /// </param>
    /// <param name="args">
    ///     The args.
    /// </param>
    /// <param name="implementationMethod">
    ///     The implementation method.
    /// </param>
    /// <returns>
    ///     The <see cref="string" />.
    /// </returns>
    public virtual string GenerateMethodNameWithArguments(MethodInfo targetMethod, object[] args,
        out MethodInfo implementationMethod)
    {
        if (!JamesConsulting.Constants.TypeMethods.ContainsKey(ObjectType))
        {
            Logger.LogDebug($"Added {ObjectType.FullName} methods to cache");
            JamesConsulting.Constants.TypeMethods[ObjectType] = ObjectType.GetMethods();
        }

        var methodName = targetMethod.IsGenericMethod
            ? targetMethod.GetGenericMethodDefinition().ToString()
            : targetMethod.ToString();
        implementationMethod =
            JamesConsulting.Constants.TypeMethods[ObjectType].Single(x => x.ToString() == methodName);
        return implementationMethod.ToInvocationString(args);
    }

    /// <summary>
    ///     The invoke.
    /// </summary>
    /// <param name="targetMethod">
    ///     The target method.
    /// </param>
    /// <param name="args">
    ///     The args.
    /// </param>
    /// <returns>
    ///     The <see cref="object" />.
    /// </returns>
    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        var aspectContext = GenerateAspectContext(targetMethod, args);

        Logger.LogDebug($"AspectContext generated");

        if (ShouldIntercept(aspectContext))
        {
            PreInvoke(aspectContext);
            var isAsync = targetMethod.IsAsync();

            if (aspectContext.InvokeMethod)
            {
                Logger.LogDebug($"Invoking {aspectContext.InvocationString}");
                Invoke(aspectContext);
            }

            if (!isAsync)
            {
                PostInvoke(aspectContext);
            }
        }
        else
        {
            Logger.LogDebug($"Invoking {aspectContext.InvocationString} without interception");
            Invoke(aspectContext);
        }

        return aspectContext.ReturnValue;
    }

    /// <summary>
    ///     The post invoke.
    /// </summary>
    /// <param name="aspectContext">
    ///     The aspect Context.
    /// </param>
    public virtual void PostInvoke(AspectContext aspectContext)
    {
    }

    /// <summary>
    ///     The pre invoke.
    /// </summary>
    /// <param name="aspectContext">
    ///     The aspect Context.
    /// </param>
    public virtual void PreInvoke(AspectContext aspectContext)
    {
    }

    /// <summary>
    ///     The should intercept.
    /// </summary>
    /// <returns>
    ///     The <see cref="bool" />.
    /// </returns>
    public virtual bool ShouldIntercept(AspectContext aspectContext)
    {
        return AspectConfigurationProvider.ShouldIntercept(FactoryType, aspectContext.TargetMethod.DeclaringType,
            ObjectType, aspectContext.TargetMethod);
    }

    /// <summary>
    ///     The call process function.
    /// </summary>
    /// <param name="aspectContext">
    ///     The aspect context.
    /// </param>
    private void CallProcessFunction(AspectContext aspectContext)
    {
        var resultType = aspectContext.TargetMethod.ReturnType.GetGenericArguments()[0];
        var mi = ProcessFunctionMethodInfo.MakeGenericMethod(resultType);
        var task = aspectContext.TargetMethod.Invoke(Instance, aspectContext.ParameterValues);
        aspectContext.ReturnValue = mi.Invoke(this, new[] { task, aspectContext });
    }

    /// <summary>
    ///     The invoke.
    /// </summary>
    /// <param name="aspectContext">
    ///     The aspect context.
    /// </param>
    private void Invoke(AspectContext aspectContext)
    {
        switch (aspectContext.MethodType)
        {
            case MethodTypeOptions.AsyncAction:
                ProcessAction(aspectContext);
                break;
            case MethodTypeOptions.AsyncFunction:
                CallProcessFunction(aspectContext);
                break;
            default:
                Process(aspectContext);
                break;
        }
    }

    /// <summary>
    ///     The process.
    /// </summary>
    /// <param name="aspectContext">
    ///     The aspect Context.
    /// </param>
    private void Process(AspectContext aspectContext)
    {
        aspectContext.ReturnValue = aspectContext.TargetMethod.Invoke(Instance, aspectContext.ParameterValues);
    }

    /// <summary>
    ///     The process action async.
    /// </summary>
    /// <param name="aspectContext">
    ///     The aspect context.
    /// </param>
    /// <returns>
    ///     The <see cref="Task" />.
    /// </returns>
    private void ProcessAction(AspectContext aspectContext)
    {
        aspectContext.ReturnValue = aspectContext.TargetMethod.Invoke(Instance, aspectContext.ParameterValues);
        var task = (Task)aspectContext.ReturnValue;
        task.ContinueWith((_, state) => PostInvoke((AspectContext)state), aspectContext);
    }

    /// <summary>
    ///     The process function async.
    /// </summary>
    /// <param name="task">
    ///     The task.
    /// </param>
    /// <param name="aspectContext">
    ///     The aspect context.
    /// </param>
    /// <typeparam name="TK">
    /// </typeparam>
    /// <returns>
    ///     The <see cref="Task{TK}" />.
    /// </returns>

    // ReSharper disable once UnusedMember.Local
#pragma warning disable S1144 // Unused private types or members should be removed
    private async Task<TK> ProcessFunctionAsync<TK>(Task<TK> task, AspectContext aspectContext)
    {
        try
        {
            aspectContext.ReturnValue = await task;
            return (TK)aspectContext.ReturnValue;
        }
        finally
        {
            PostInvoke(aspectContext);
        }
    }

#pragma warning restore S1144 // Unused private types or members should be removed
    }
}