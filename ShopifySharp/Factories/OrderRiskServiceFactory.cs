// Notice:
// This class is auto-generated from a template. Please do not edit it or change it directly.
#if NETSTANDARD2_0
#nullable disable
#else
#nullable enable
#endif

using ShopifySharp.Credentials;

namespace ShopifySharp.Factories;

public interface IOrderRiskServiceFactory
{
    /// Creates a new instance of the <see cref="IOrderRiskService" /> with the given credentials.
    /// <param name="shopDomain">The shop's *.myshopify.com URL.</param>
    /// <param name="accessToken">An API access token for the shop.</param>
    IOrderRiskService Create(string shopDomain, string accessToken);

    /// Creates a new instance of the <see cref="IOrderRiskService" /> with the given credentials.
    /// <param name="credentials">Credentials for authenticating with the Shopify API.</param>
    IOrderRiskService Create(ShopifyApiCredentials credentials);
}

public class OrderRiskServiceFactory(
    #if NETSTANDARD2_0
    IRequestExecutionPolicy requestExecutionPolicy = null
    #else
    IRequestExecutionPolicy? requestExecutionPolicy = null
    #endif
) : IOrderRiskServiceFactory
{
    /// <inheritDoc />
    public virtual IOrderRiskService Create(string shopDomain, string accessToken)
    {
        var service = new OrderRiskService(shopDomain, accessToken);

        if (requestExecutionPolicy is not null)
        {
            service.SetExecutionPolicy(requestExecutionPolicy);
        }

        return service;
    }

    /// <inheritDoc />
    public virtual IOrderRiskService Create(ShopifyApiCredentials credentials) =>
        Create(credentials.ShopDomain, credentials.AccessToken);
}
