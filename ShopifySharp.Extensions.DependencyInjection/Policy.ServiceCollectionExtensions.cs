using System;
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
        services.TryAddPolicyConfigurationOptions();
        services.Add(new ServiceDescriptor(typeof(IRequestExecutionPolicy), typeof(T), lifetime));
        return services;
    }

    public static IServiceCollection AddShopifySharpRequestExecutionPolicyOptions<TOptions>(this IServiceCollection services, Action<TOptions> configure)
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        return services
            .TryAddDefaultPolicyOptions<TOptions>()
            .Configure(configure)
            .PostConfigure<IRequestExecutionPolicyOptions>(x => x.Validate());
    }

    private static IServiceCollection TryAddDefaultPolicyOptions<TOptions>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        services.Configure()
        services.TryAdd(
        );

        return services;

        static ServiceDescriptor CreateServiceDescriptor<T>(T optionsValue, string serviceKey, ServiceLifetime lifetime)
            where T : class, new() =>
            new (typeof(IOptions<T>), serviceKey, (sp, key) =>
            {
                IOptions<T> options = new OptionsWrapper<T>(optionsValue);
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
