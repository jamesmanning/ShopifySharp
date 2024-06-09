using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ShopifySharp.Infrastructure.Policies.ExponentialRetry;

namespace ShopifySharp.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    private const string PolicyKey = "ShopifySharp.DI.Policy";
    private const string PolicyOptionsKey = "ShopifySharp.DI.PolicyOptions";

    /// <summary>
    /// Adds the <see cref="IRequestExecutionPolicy"/> to your Dependency Injection container. ShopifySharp service factories
    /// managed by your container will use this policy when creating ShopifySharp services.
    /// <p>Note: Policies are not true middleware, ShopifySharp services can only use one policy at this time.</p>2
    /// </summary>
    /// <param name="services"></param>
    /// <param name="lifetime">The lifetime of <see cref="IRequestExecutionPolicy"/>.</param>
    /// <typeparam name="TPolicy">A class that implements ShopifySharp's <see cref="IRequestExecutionPolicy"/> interface.</typeparam>
    public static IServiceCollection AddShopifySharpRequestExecutionPolicy<TPolicy>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TPolicy : class, IRequestExecutionPolicy
    {
        return services.TryAddPolicyFactory<TPolicy>(lifetime);
        // services.TryAddPolicyOptionFactories(lifetime)
        //     .TryAddPolicyFactory<TPolicy>(lifetime);
        //
        // services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy), PolicyKey, typeof(TPolicy), lifetime));
        // services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy), null, (sp, key) =>
        // {
        //     // Prefer using the policy factory when possible, because the factories will receive any DI services they need
        //     var policyFactory = sp.GetService<IRequestExecutionPolicyFactory<TPolicy>>();
        //     return policyFactory is not null
        //         ? policyFactory.Create()
        //         : sp.GetRequiredKeyedService<IRequestExecutionPolicy>(PolicyKey);
        // }, lifetime));
        //
        // return services;
    }

    public static IServiceCollection AddShopifySharpRequestExecutionPolicyOptions<TOptions>(this IServiceCollection services, Action<TOptions> configure, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        return services.TryAddPolicyOptionsFactory<TOptions>(lifetime)
            .Configure(configure)
            .PostConfigure<TOptions>(x => x.Validate());
    }

    private static IServiceCollection TryAddPolicyFactory<TPolicy>(this IServiceCollection services, ServiceLifetime lifetime)
        where TPolicy : class, IRequestExecutionPolicy
    {
        // Step 1: Register the default policy options if the TPolicy is a type that requires options
        // Step 2: Register policy options factory if the TPolicy is a type that requires options
        // Step 3: Register the IRequestExecutionPolicy and policy class itself
        // Step 4: Register the policy factory, which pulls the policy from DI (and the policy pulls its options from DI as well.)

        if (TryGetPolicyOptionsType<TPolicy>(out var policyOptionsType))
        {
            Console.WriteLine("policy type is {0}", policyOptionsType.Name);
            // TODO: make this generic so it's future proof/works with any custom TPolicy that needs options. Add an interface
            //       named IRequiresPolicyOptions<TPolicyOptions> so we can get a reference to the options type.
            // TODO: check if the keyed services stuff can be simplified somehow – if services of different types share the
            //       same key, can they automatically instantiate each other like normal DI?
            // TODO: investigate using a keyed service provider if the shared key doesn't pan out.
                     // public static void AddPolicy(this IServiceCollection services)
                     // {
                     //     // Register the keyed PolicyOptions
                     //     services.AddKeyedSingleton("FooOptions", new PolicyOptions { Foos = 42 });
                     //
                     //     // Register Policy as singleton and resolve its dependencies in a separate setup
                     //     services.AddSingleton<Policy, KeyedPolicyResolver>();
                     // }
                     //
                     // public class KeyedPolicyResolver : Policy
                     // {
                     //     public KeyedPolicyResolver(IServiceProvider provider)
                     //         : base(provider.GetKeyedService<PolicyOptions>("FooOptions"))
                     //     {
                     //     }
                     // }

            services.TryAddPolicyOptionsFactory<>()

            // Use a completely separate ServiceDescriptor factory to create the policy and policy factory when the
            // policy type requires options.
            services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy), PolicyKey, (sp, _) =>
            {
                var options = sp.GetRequiredKeyedService<ExponentialRetryPolicyOptions>(PolicyOptionsKey);
                return new ExponentialRetryPolicy(options);
            }, lifetime));
        }
        else
        {
            services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy), PolicyKey, typeof(TPolicy), lifetime));
        }

        services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy),
            null,
            (sp, _) => sp.GetRequiredKeyedService<IRequestExecutionPolicy>(PolicyKey),
            lifetime));

        return services;
    }

    private static IServiceCollection TryAddPolicyOptionsFactory<TOptions>(this IServiceCollection services, ServiceLifetime lifetime)
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        return services.TryAddPolicyOptionsFactory(typeof(TOptions), lifetime);
    }

    private static IServiceCollection TryAddPolicyOptionsFactory(this IServiceCollection services, Type policyOptionsType, ServiceLifetime lifetime)
    {
        var defaultOptionsKey = GetDefaultPolicyOptionsKey(policyOptionsType);

        services.TryAdd(new ServiceDescriptor(policyOptionsType, defaultOptionsKey, (_, _) => TOptions.Default(), lifetime));
        services.TryAdd(new ServiceDescriptor(policyOptionsType, PolicyOptionsKey, (sp, _) =>
        {
            // Always prefer to use IOptions<TOptions> if available
            if (sp.GetService(Type.MakeGenericSignatureType(typeof(IOptions<>), policyOptionsType)) is IOptions<object> optionsWrappedValue)
                return optionsWrappedValue.Value;

            var rawValue = sp.GetService<TOptions>();
            return rawValue ?? sp.GetRequiredKeyedService<TOptions>(defaultOptionsKey);
        }, lifetime));

        return services;
    }

    private static string GetDefaultPolicyOptionsKey(Type policyOptionsType)
    {
        const string keyPrefix = "ShopifySharp.Extensions.DependencyInjection.PolicyOptions.Default.";
        return keyPrefix + policyOptionsType.Name;
    }

    private static bool TryGetPolicyOptionsType<TPolicy>([NotNullWhen(true)] out Type? optionsType)
        where TPolicy : IRequestExecutionPolicy
    {
        optionsType = null;

        foreach (var type in typeof(TPolicy).GetInterfaces())
        {
            if (!type.IsGenericType || !type.IsInterface)
                continue;

            var interfaces = type.GetInterfaces();

            if (!interfaces.Contains(typeof(IRequestExecutionPolicyRequiresOptions)))
                continue;

            var genericArguments = type.GetGenericArguments();

            if (genericArguments.Length != 1)
                continue;

            var targetType = typeof(IRequestExecutionPolicyRequiresOptions<>).MakeGenericType(genericArguments);

            if (targetType != type)
                continue;

            optionsType = genericArguments[0];
            return true;
        }

        return false;
    }
}
