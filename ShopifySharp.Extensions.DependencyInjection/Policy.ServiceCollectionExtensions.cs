using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ShopifySharp.Factories.Policies;
using ShopifySharp.Infrastructure.Policies.ExponentialRetry;

namespace ShopifySharp.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    private const string PolicyKey = "ShopifySharp.DI.Policy";

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
        services.TryAddPolicyOptionFactories(lifetime)
            .TryAddPolicyFactory<TPolicy>(lifetime);

        services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy), PolicyKey, typeof(TPolicy), lifetime));
        services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy), null, (sp, key) =>
        {
            // Prefer using the policy factory when possible, because the factories will receive any DI services they need
            var policyFactory = sp.GetService<IRequestExecutionPolicyFactory<TPolicy>>();
            return policyFactory is not null
                ? policyFactory.Create()
                : sp.GetRequiredKeyedService<IRequestExecutionPolicy>(PolicyKey);
        }, lifetime));

        return services;
    }

    public static IServiceCollection AddShopifySharpRequestExecutionPolicyOptions<TOptions>(this IServiceCollection services, Action<TOptions> configure)
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        return services
            .TryAddPolicyOptionFactories(ServiceLifetime.Singleton)
            .Configure(configure)
            .PostConfigure<TOptions>(x => x.Validate());
    }

    private static IServiceCollection TryAddPolicyFactory<TPolicy>(this IServiceCollection services, ServiceLifetime lifetime)
        where TPolicy : class, IRequestExecutionPolicy
    {
        const string serviceKey = "ShopifySharp.DI.DesiredPolicy.Factory";

        // TODO:
        // Step 1: Register policy options factory if the TPolicy is a type that requires options
        // Step 2: Register the IRequestExecutionPolicy and policy class itself
        // Step 3: Register the policy factory, which pulls the policy from DI (and the policy pulls its options from DI as well.)

        if (typeof(TPolicy) == typeof(ExponentialRetryPolicy))
        {

        }

        services.TryAdd(new ServiceDescriptor(typeof(IRequestExecutionPolicy), serviceKey, (sp, _) =>
        {
            var existingPolicy = sp.GetService<TPolicy>();
            if (existingPolicy is not null)
                return existingPolicy;

            // TODO: find some way to see if we can call `new TPolicy()` *and* `new TPolicy(policyOptions)` here
            if (typeof(TPolicy) == typeof(ExponentialRetryPolicy))
            {
                var p = new ExponentialRetryPolicy(ExponentialRetryPolicyOptions.Default());
            }
            else
            {

            }

            return new DefaultRequestExecutionPolicy();
        }, lifetime));

        return services;
    }

    private static IServiceCollection TryAddDefaultPolicyOptions(this IServiceCollection services, ServiceLifetime lifetime)
    {
        var assembly = Assembly.GetAssembly(typeof(IRequestExecutionPolicyOptions<>));
        var factoryTypes = assembly!.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestExecutionPolicyOptions<>)));

        foreach (var type in factoryTypes)
        {
            var serviceType = type.GetInterfaces()
                .FirstOrDefault(i => !i.IsGenericType);

            if(serviceType != null)
            {
                services.TryAdd(new ServiceDescriptor(serviceType, null, (sp, _) =>
                {

                }), lifetime);

                if (typeof(TPolicy) == typeof(IRequestExecutionPolicyOptions<ExponentialRetryPolicyOptions>))
                {

                }
            }
        }

        return services;
    }

    private static IServiceCollection TryAddPolicyOptionFactories(this IServiceCollection services, ServiceLifetime lifetime)
    {
        // Add more policy option types here as they become available
        return TryAdd<ExponentialRetryPolicyOptions>(services, lifetime);

        static IServiceCollection TryAdd<TPolicyOptions>(IServiceCollection services, ServiceLifetime lifetime)
            where TPolicyOptions : class, IRequestExecutionPolicyOptions<TPolicyOptions>, new()
        {
            services.TryAdd(new ServiceDescriptor(typeof(TPolicyOptions), null, (sp, _) =>
            {
                var userOptionsFromConfig = sp.GetService<IOptions<TPolicyOptions>>();
                if (userOptionsFromConfig is not null)
                    return userOptionsFromConfig.Value;

                var userOptionsFromExtensionMethod = sp.GetKeyedService<TPolicyOptions>(GetCustomPolicyOptionsKey<TPolicyOptions>());
                return userOptionsFromExtensionMethod ?? sp.GetRequiredKeyedService<TPolicyOptions>(GetDefaultPolicyOptionsKey<TPolicyOptions>());
            }, lifetime));

            return services;
        }
    }

    private static string GetDefaultPolicyOptionsKey<TOptions>()
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        const string keyPrefix = "ShopifySharp.Extensions.DependencyInjection.PolicyOptions.Default.";
        var type = typeof(TOptions);
        return keyPrefix + type.Name;
    }

    private static string GetCustomPolicyOptionsKey<TOptions>()
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        const string keyPrefix = "ShopifySharp.Extensions.DependencyInjection.PolicyOptions.Custom.";
        var type = typeof(TOptions);
        return keyPrefix + type.Name;
    }
}
