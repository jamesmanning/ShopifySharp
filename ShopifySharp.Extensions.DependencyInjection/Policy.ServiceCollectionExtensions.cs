using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ShopifySharp.Infrastructure.Policies;

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
        services.TryAddPolicyFactory<TPolicy>(lifetime);
        return services;
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
        Action<TOptions>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TPolicy : class, IRequestExecutionPolicy, IRequestExecutionPolicyRequiresOptions<TOptions>
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        services.AddOptions();
        services.TryAddTransient<IOptionsFactory<TOptions>, DefaultPolicyOptionsFactory<TOptions>>();

        if (configure is not null)
            services.Configure(configure);

        services.TryAddPolicyFactory<TPolicy>(lifetime);
        services.TryAddPolicyOptionsFactory<TOptions>(lifetime);
        services.PostConfigureAll<TOptions>(x => x.Validate());

        return services;
    }

    private static void TryAddPolicyFactory<TPolicy>(this IServiceCollection services, ServiceLifetime lifetime)
        where TPolicy : class, IRequestExecutionPolicy
    {
        services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy), PolicyKey, typeof(TPolicy), lifetime));
        services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy),
            null,
            (sp, _) => sp.GetRequiredKeyedService<IRequestExecutionPolicy>(PolicyKey),
            lifetime));
    }

    private static void TryAddPolicyOptionsFactory<TOptions>(this IServiceCollection services, ServiceLifetime lifetime)
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        services.TryAdd(new ServiceDescriptor(typeof(TOptions), null, (sp, _) =>
        {
            // Always prefer to use options that were configured explicitly when possible
            if (sp.GetKeyedService<TOptions>(PolicyOptionsKey) is not null and var keyedOptions)
                return keyedOptions;

            // Else, fallback to any IOptions that may be injected via configuration
            return sp.GetRequiredService<IOptions<TOptions>>().Value;
        }, lifetime));
    }

    private static string GetDefaultPolicyOptionsKey(Type policyOptionsType)
    {
        const string keyPrefix = "ShopifySharp.Extensions.DependencyInjection.PolicyOptions.Default.";
        return keyPrefix + policyOptionsType.Name;
    }
}

public class DefaultPolicyOptionsFactory<TOptions>(
    IEnumerable<IConfigureOptions<TOptions>> setups,
    IEnumerable<IPostConfigureOptions<TOptions>> postConfigures
) : IOptionsFactory<TOptions>
    where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
{
    public TOptions Create(string name)
    {
        var options = TOptions.Default();

        foreach (var setup in setups)
        {
            if (setup is IConfigureNamedOptions<TOptions> configureNamedOptions)
                configureNamedOptions.Configure(name, options);
            else
                setup.Configure(options);
        }

        foreach (var postConfigure in postConfigures)
            postConfigure.PostConfigure(name, options);

        return options;
    }
}
