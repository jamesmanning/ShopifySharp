using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ShopifySharp.Factories;
using ShopifySharp.Utilities;
using System.Reflection;
using System.Linq;
using ShopifySharp.Infrastructure.Policies;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace ShopifySharp.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ShopifySharp's utilities to your Dependency Injection container. Includes the following utilities:
    /// <list type="bullet">
    /// <item><see cref="IShopifyOauthUtility"/></item>
    /// <item><see cref="IShopifyDomainUtility"/></item>
    /// <item><see cref="IShopifyRequestValidationUtility"/></item>
    /// </list>
    /// <param name="configure">An optional configuration action for configuring the utilities.</param>
    /// <param name="lifetime">The lifetime of the ShopifySharp's utilities.</param>
    /// </summary>
    public static IServiceCollection AddShopifySharpUtilities(this IServiceCollection services,
        Action<ShopifySharpUtilityOptions>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var options = new ShopifySharpUtilityOptions();
        configure?.Invoke(options);

        if(options.OauthUtility != null)
        {
            services.Add(new ServiceDescriptor(typeof(IShopifyOauthUtility), f => options.OauthUtility, lifetime));
        }
        else
        {
            services.TryAdd(new ServiceDescriptor(typeof(IShopifyOauthUtility), typeof(ShopifyOauthUtility), lifetime));
        }

        if(options.DomainUtility != null)
        {
            services.Add(new ServiceDescriptor(typeof(IShopifyDomainUtility), f => options.DomainUtility, lifetime));
        }
        else
        {
            services.TryAdd(new ServiceDescriptor(typeof(IShopifyDomainUtility), typeof(ShopifyDomainUtility), lifetime));
        }

        if(options.RequestValidationUtility != null)
        {
            services.Add(new ServiceDescriptor(typeof(IShopifyRequestValidationUtility), f => options.RequestValidationUtility, lifetime));
        }
        else
        {
            services.TryAdd(new ServiceDescriptor(typeof(IShopifyRequestValidationUtility), typeof(ShopifyRequestValidationUtility), lifetime));
        }

        return services;
    }

    /// <summary>
    /// Adds ShopifySharp's service factories to your Dependency Injection container. If you've added an <see cref="IRequestExecutionPolicy"/>,
    /// the service factories will use it when creating ShopifySharp services.
    /// </summary>
    /// <param name="lifetime">The lifetime of ShopifySharp's service factories.</param>
    public static IServiceCollection AddShopifySharpServiceFactories(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var assembly = Assembly.GetAssembly(typeof(IServiceFactory<>));

        var factoryTypes = assembly!.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IServiceFactory<>)));

        foreach (var type in factoryTypes)
        {
            var serviceType = type
                .GetInterfaces()
                .FirstOrDefault(i => !i.IsGenericType);

            if(serviceType != null)
            {
                services.TryAdd(new ServiceDescriptor(serviceType, type, lifetime));
            }
        }

        services.TryAdd(new ServiceDescriptor(typeof(IPartnerServiceFactory), typeof(PartnerServiceFactory), lifetime));

        return services;
    }

    /// <summary>
    /// Adds all of ShopifySharp's Dependency Injection services to your DI container. This is a convenience method and
    /// simply calls the following extensions sequentially:
    /// <list type="bullet">
    /// <item><see cref="AddShopifySharpRequestExecutionPolicy{T}(IServiceCollection,ServiceLifetime)"/></item>
    /// <item><see cref="AddShopifySharpUtilities"/></item>
    /// </list>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="lifetime">The lifetime of all ShopifySharp's Dependency Injection services</param>
    /// <typeparam name="TPolicy"></typeparam>
    public static IServiceCollection AddShopifySharp<TPolicy>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TPolicy : class, IRequestExecutionPolicy
    {
        return services
            .AddShopifySharpRequestExecutionPolicy<TPolicy>(lifetime)
            .AddShopifySharpUtilities(lifetime: lifetime)
            .AddShopifySharpServiceFactories(lifetime: lifetime);
    }

    /// <summary>
    /// Adds all of ShopifySharp's Dependency Injection services to your DI container. This is a convenience method and
    /// simply calls the following extensions sequentially:
    /// <list type="bullet">
    /// <item><see cref="AddShopifySharpRequestExecutionPolicy{T}(IServiceCollection,ServiceLifetime)"/></item>
    /// <item><see cref="AddShopifySharpUtilities"/></item>
    /// </list>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure">A function used to configure the options for the execution policy.</param>
    /// <param name="lifetime">The lifetime of all ShopifySharp's Dependency Injection services</param>
    /// <typeparam name="TPolicy">A class that implements ShopifySharp's <see cref="IRequestExecutionPolicy"/> interface.</typeparam>
    /// <typeparam name="TOptions"></typeparam>
    public static IServiceCollection AddShopifySharp<TPolicy, TOptions>(
        this IServiceCollection services,
        Action<TOptions> configure,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TPolicy : class, IRequestExecutionPolicy, IRequestExecutionPolicyRequiresOptions<TOptions>
        where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
    {
        return services
            .AddShopifySharpRequestExecutionPolicy<TPolicy, TOptions>(configure, lifetime)
            .AddShopifySharpUtilities(lifetime: lifetime)
            .AddShopifySharpServiceFactories(lifetime: lifetime);
    }
}
