using System;
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

        if (typeof(TPolicy) == typeof(ExponentialRetryPolicy))
        {

            services.TryAddPolicyOptionsFactory<ExponentialRetryPolicyOptions>(lifetime);
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
        var defaultOptionsKey = GetDefaultPolicyOptionsKey<TOptions>();

        services.TryAdd(new ServiceDescriptor(typeof(TOptions), defaultOptionsKey, (_, _) => TOptions.Default(), lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(TOptions), PolicyOptionsKey, (sp, _) =>
        {
            // Always prefer to use IOptions<TOptions> if available
            var optionsWrappedValue = sp.GetService<IOptions<TOptions>>();
            if (optionsWrappedValue is not null)
                return optionsWrappedValue.Value;

            var rawValue = sp.GetService<TOptions>();
            return rawValue ?? sp.GetRequiredKeyedService<TOptions>(defaultOptionsKey);
        }, lifetime));

        return services;
    }

    private static string GetDefaultPolicyOptionsKey<TOptions>()
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        const string keyPrefix = "ShopifySharp.Extensions.DependencyInjection.PolicyOptions.Default.";
        var type = typeof(TOptions);
        return keyPrefix + type.Name;
    }
}
