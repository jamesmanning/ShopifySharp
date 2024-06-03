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
    private const string DefaultExponentialRetryPolicyOptionsKeyPrefix = "ShopifySharp.DI.PolicyOptions.Default.";
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
        services.TryAddDefaultPolicyOptions(lifetime)
            .TryAddPolicyFactories(lifetime);
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
            .TryAddDefaultPolicyOptions(ServiceLifetime.Singleton)
            .Configure(configure)
            .PostConfigure<TOptions>(x => x.Validate());
    }

    private static IServiceCollection TryAddPolicyFactories(this IServiceCollection services, ServiceLifetime lifetime)
    {
        var assembly = Assembly.GetAssembly(typeof(IRequestExecutionPolicyFactory));
        var factoryTypes = assembly!.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestExecutionPolicyFactory)));

        foreach (var type in factoryTypes)
        {
            var serviceType = type.GetInterfaces()
                .FirstOrDefault(i => !i.IsGenericType);

            if(serviceType != null)
            {
                services.TryAdd(new ServiceDescriptor(serviceType, type, lifetime));
            }
        }

        return services;
    }

    private static IServiceCollection TryAddDefaultPolicyOptions(this IServiceCollection services, ServiceLifetime lifetime)
    {
        // Add more policy option types here as they become available
        return TryAdd<ExponentialRetryPolicyOptions>(services, lifetime);

        static IServiceCollection TryAdd<TOptions>(IServiceCollection services, ServiceLifetime lifetime)
            where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
        {
            var defaultServiceKey = GetKeyedDefaultOptionsKey<TOptions>();
            var defaultValues = TOptions.Default();

            services.TryAdd(new ServiceDescriptor(typeof(IOptions<TOptions>), defaultServiceKey, (_, _) =>
            {
                IOptions<TOptions> options = new OptionsWrapper<TOptions>(defaultValues);
                return options;
            }, lifetime));

            return services;
        }
    }

    private static string GetKeyedDefaultOptionsKey<TOptions>()
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        var type = typeof(TOptions);
        return DefaultExponentialRetryPolicyOptionsKeyPrefix + type.Name;
    }
}
