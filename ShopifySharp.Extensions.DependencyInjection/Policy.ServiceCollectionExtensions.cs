using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ShopifySharp.Infrastructure.Policies.ExponentialRetry;

namespace ShopifySharp.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    private const string DefaultExponentialRetryPolicyOptionsKeyPrefix = "ShopifySharp.DI.PolicyOptions.Default.";

    /// <summary>
    /// Adds the <see cref="IRequestExecutionPolicy"/> to your Dependency Injection container. ShopifySharp service factories
    /// managed by your container will use this policy when creating ShopifySharp services.
    /// <p>Note: Policies are not true middleware, ShopifySharp services can only use one policy at this time.</p>2
    /// </summary>
    /// <param name="services"></param>
    /// <param name="lifetime">The lifetime of <see cref="IRequestExecutionPolicy"/>.</param>
    /// <typeparam name="T">A class that implements ShopifySharp's <see cref="IRequestExecutionPolicy"/> interface.</typeparam>
    public static IServiceCollection AddShopifySharpRequestExecutionPolicy<T>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where T : class, IRequestExecutionPolicy
    {
        return services.TryAddDefaultPolicyOptions(lifetime)
            .AddPolicyFactory(lifetime);
    }

    public static IServiceCollection AddShopifySharpRequestExecutionPolicyOptions<TOptions>(this IServiceCollection services, Action<TOptions> configure)
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        return services
            .TryAddDefaultPolicyOptions(ServiceLifetime.Singleton)
            .Configure(configure)
            .PostConfigure<TOptions>(x => x.Validate());
    }

    private static IServiceCollection TryAddDefaultPolicyOptions(this IServiceCollection services, ServiceLifetime lifetime)
    {
        // Add more policy option types here as they become available
        services.TryAdd([CreateServiceDescriptor<ExponentialRetryPolicyOptions>(lifetime)]);
        return services;
    }

    private static IServiceCollection AddPolicyFactory(this IServiceCollection services, ServiceLifetime lifetime)
    {
        return services;

        static List<ServiceDescriptor> CreatePolicyDescriptor<TPolicy, TPolicyOptions>(ServiceLifetime lifetime)
            where TPolicy : class, IRequestExecutionPolicy
            where TPolicyOptions : class, IRequestExecutionPolicyOptions<TPolicyOptions>, new()
        {
            return
            [
                new ServiceDescriptor(typeof(IRequestExecutionPolicy), null, (sp, key) =>
                {
                    var options = sp.GetRequiredService<TPolicyOptions>();
                    return options.CreatePolicy();
                }, lifetime)
            ];
        }

        services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy), null, (sp, key) =>
        {
            var options = sp.GetService<IOptions<ExponentialRetryPolicyOptions>>();
            if (options is not null)
                return new ExponentialRetryPolicy(options.Value);

            var existingOptions = sp.GetService<ExponentialRetryPolicyOptions>();
            if (existingOptions is not null)
                return new ExponentialRetryPolicy(existingOptions);

            var defaultOptionsKey = GetKeyedDefaultOptionsKey<ExponentialRetryPolicyOptions>();
            var defaultOptions = sp.GetRequiredKeyedService<ExponentialRetryPolicyOptions>(defaultOptionsKey);
            return new ExponentialRetryPolicy(defaultOptions);
        }, lifetime));

        return services;
    }

    private static ServiceDescriptor CreateServiceDescriptor<TOptions>(ServiceLifetime lifetime)
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        var defaultServiceKey = GetKeyedDefaultOptionsKey<TOptions>();
        var defaultValues = TOptions.Default();

        return new ServiceDescriptor(typeof(IOptions<TOptions>), defaultServiceKey, (_, _) =>
        {
            IOptions<TOptions> options = new OptionsWrapper<TOptions>(defaultValues);
            return options;
        }, lifetime);
    }

    private static string GetKeyedDefaultOptionsKey<TOptions>()
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        var type = typeof(TOptions);
        return DefaultExponentialRetryPolicyOptionsKeyPrefix + type.Name;
    }
}
