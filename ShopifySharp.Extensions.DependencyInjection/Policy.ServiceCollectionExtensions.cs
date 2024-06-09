using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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

    /// <summary>
    /// Adds the <see cref="IRequestExecutionPolicy"/> to your Dependency Injection container. ShopifySharp service factories
    /// managed by your container will use this policy when creating ShopifySharp services.
    /// <p>Note: Policies are not true middleware, ShopifySharp services can only use one policy at this time.</p>2
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure">A function used to configure the options for the execution policy.</param>
    /// <param name="lifetime">The lifetime of <see cref="IRequestExecutionPolicy"/>.</param>
    /// <typeparam name="TPolicy">A class that implements ShopifySharp's <see cref="IRequestExecutionPolicy"/> interface.</typeparam>
    /// <typeparam name="TOptions"></typeparam>
    public static IServiceCollection AddShopifySharpRequestExecutionPolicy<TPolicy, TOptions>(this IServiceCollection services,
        Action<TOptions> configure,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TPolicy : class, IRequestExecutionPolicy, IRequestExecutionPolicyRequiresOptions<TOptions>
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        // TODO: make this use IOptions<T>

        var defaultValue = TOptions.Default();
        configure(defaultValue);
        defaultValue.Validate();

        services.TryAddPolicyFactory<TPolicy>(lifetime)
            .Add(new ServiceDescriptor(typeof(TOptions), PolicyKey, (_, _) => defaultValue, lifetime));

        return services.TryAddPolicyOptionsFactory<TOptions>(lifetime);
    }

    private static IServiceCollection TryAddPolicyFactory<TPolicy>(this IServiceCollection services, ServiceLifetime lifetime)
        where TPolicy : class, IRequestExecutionPolicy
    {
        services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy), PolicyKey, typeof(TPolicy), lifetime));
        services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy),
            null,
            (sp, _) => sp.GetRequiredKeyedService<IRequestExecutionPolicy>(PolicyKey),
            lifetime));

        return services;
    }

    private static IServiceCollection TryAddPolicyOptionsFactory<TOptions>(this IServiceCollection services, ServiceLifetime lifetime)
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        var defaultOptionsKey = GetDefaultPolicyOptionsKey(typeof(TOptions));

        // TODO: make this use IOptions<T> where it's injecting TOptions.Default() and looking for TOptions from services

        services.TryAdd(new ServiceDescriptor(typeof(TOptions), defaultOptionsKey, (_, _) => TOptions.Default(), lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(TOptions), null, (sp, _) =>
        {
            // Always prefer to use options that were configured explicitly when possible
            if (sp.GetKeyedService<TOptions>(PolicyOptionsKey) is not null and var keyedOptions)
                return keyedOptions;

            // Else, fallback to any IOptions that may be injected via configuration
            // TODO: test when this is null and when it's not null – it seems to be creating a default TOptions instance with empty values
            if (sp.GetService<IOptions<TOptions>>() is not null and var optionsWrappedValue)
                return optionsWrappedValue.Value;

            return  sp.GetRequiredKeyedService<TOptions>(defaultOptionsKey);
        }, lifetime));

        return services;
    }

    private static string GetDefaultPolicyOptionsKey(Type policyOptionsType)
    {
        const string keyPrefix = "ShopifySharp.Extensions.DependencyInjection.PolicyOptions.Default.";
        return keyPrefix + policyOptionsType.Name;
    }
}
