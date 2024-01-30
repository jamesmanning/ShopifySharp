#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ShopifySharp.Enums;
using ShopifySharp.Infrastructure;

namespace ShopifySharp.Utilities;

public interface IShopifyOauthUtility
{
    /// <summary>
    /// Builds an OAuth authorization URL for Shopify OAuth integration.
    /// </summary>
    /// <param name="scopes">An array of <see cref="AuthorizationScope"/> — the permissions that your app needs to run.</param>
    /// <param name="shopDomain">The shop's *.myshopify.com URL.</param>
    /// <param name="clientId">Your app's public Client ID, also known as its public API key.</param>
    /// <param name="redirectUrl">URL to redirect the user to after integration.</param>
    /// <param name="state">An optional, random string value provided by your application which is unique for each authorization request. During the OAuth callback phase, your application should check that this value matches the one you provided to this method.</param>
    /// <param name="grants">Requested grant types, which will change the type of access token granted upon OAuth completion.</param>
    Uri BuildAuthorizationUrl(
        IEnumerable<AuthorizationScope> scopes,
        string shopDomain,
        string clientId,
        string redirectUrl,
        string? state = null,
        IEnumerable<string>? grants = null
    );

    /// <summary>
    /// Builds an OAuth authorization URL for Shopify OAuth integration.
    /// </summary>
    /// <param name="scopes">An array of Shopify permission strings, e.g. 'read_orders' or 'write_script_tags'. These are the permissions that your app needs to run.</param>
    /// <param name="shopDomain">The shop's *.myshopify.com URL.</param>
    /// <param name="clientId">Your app's public Client ID, also known as its public API key.</param>
    /// <param name="redirectUrl">URL to redirect the user to after integration.</param>
    /// <param name="state">An optional, random string value provided by your application which is unique for each authorization request. During the OAuth callback phase, your application should check that this value matches the one you provided to this method.</param>
    /// <param name="grants">Requested grant types, which will change the type of access token granted upon OAuth completion.</param>
    Uri BuildAuthorizationUrl(
        IEnumerable<string> scopes,
        string shopDomain,
        string clientId,
        string redirectUrl,
        string? state = null,
        IEnumerable<string>? grants = null
    );

    #if NET8_0_OR_GREATER
    /// <summary>
    /// Builds an OAuth authorization URL for Shopify OAuth integration.
    /// </summary>
    /// <param name="options">Options for building the OAuth URL.</param>
    Uri BuildAuthorizationUrl(AuthorizationUrlOptions options);
    #endif

    /// <summary>
    /// Authorizes an application installation, generating an access token for the given shop.
    /// </summary>
    /// <param name="code">The authorization code generated by Shopify, which is attached to the redirect querystring when Shopify redirects the user back to your app.</param>
    /// <param name="shopDomain">The store's *.myshopify.com URL, which is attached as a parameter named <c>shop</c> on the redirect querystring.</param>
    /// <param name="clientId">Your app's public Client ID, also known as its public API key.</param>
    /// <param name="clientSecret">Your app's Client Secret, also known as its secret API key.</param>
    Task<AuthorizationResult> AuthorizeAsync(
        string code,
        string shopDomain,
        string clientId,
        string clientSecret
    );

    #if NET8_0_OR_GREATER
    /// <summary>
    /// Authorizes an application installation, generating an access token for the given shop.
    /// </summary>
    /// <param name="options">Options for performing the authorization.</param>
    Task<AuthorizationResult> AuthorizeAsync(AuthorizeOptions options);
    #endif

    /// <summary>
    /// Refreshes an existing store access token using the app's client secret and a refresh token
    /// For more info on rotating tokens, see https://shopify.dev/apps/auth/oauth/rotate-revoke-client-credentials
    /// </summary>
    /// <param name="shopDomain">The store's *.myshopify.com url</param>
    /// <param name="clientId">Your app's public Client ID, also known as its public API key.</param>
    /// <param name="clientSecret">Your app's Client Secret, also known as its secret API key.</param>
    /// <param name="refreshToken">The app's refresh token</param>
    /// <param name="existingStoreAccessToken">The existing store access token</param>
    Task<AuthorizationResult> RefreshAccessTokenAsync(
        string shopDomain,
        string clientId,
        string clientSecret,
        string refreshToken,
        string existingStoreAccessToken
    );

    #if NET8_0_OR_GREATER
    /// <summary>
    /// Refreshes an existing store access token using the app's client secret and a refresh token
    /// For more info on rotating tokens, see https://shopify.dev/apps/auth/oauth/rotate-revoke-client-credentials
    /// </summary>
    /// <param name="options">Options for refreshing the access token.</param>
    Task<AuthorizationResult> RefreshAccessTokenAsync(RefreshAccessTokenOptions options);
    #endif
}

#if NET8_0_OR_GREATER
public record AuthorizationUrlOptions
{
    /// An array of Shopify permission strings, e.g. 'read_orders' or 'write_script_tags'. These are the permissions that your app needs to run.
    public required IEnumerable<string> Scopes { get; init; }
    /// The shop's *.myshopify.com URL.
    public required string ShopDomain { get; init; }
    /// Your app's public Client ID, also known as its public API key.
    public required string ClientId { get; init; }
    /// URL to redirect the user to after integration.
    public required string RedirectUrl { get; init; }
    /// An optional, random string value provided by your application which is unique for each authorization request. During the OAuth callback phase, your application should check that this value matches the one you provided to this method.
    public string? State { get; init; }
    /// Requested grant types, which will change the type of access token granted upon OAuth completion.
    public IEnumerable<string>? Grants { get; init; }
}

public record AuthorizeOptions
{
    /// The authorization code generated by Shopify, which is attached to the redirect querystring when Shopify redirects the user back to your app.
    public required string Code { get; init; }
    /// The store's *.myshopify.com URL, which is attached as a parameter named <c>shop</c> on the redirect querystring.
    public required string ShopDomain { get; init; }
    /// Your app's public Client ID, also known as its public API key.
    public required string ClientId { get; init; }
    /// Your app's Client Secret, also known as its secret API key.
    public required string ClientSecret { get; init; }
}

public record RefreshAccessTokenOptions
{
    /// The store's *.myshopify.com url
    public required string ShopDomain { get; init; }
    /// Your app's public Client ID, also known as its public API key.
    public required string ClientId { get; init; }
    /// Your app's Client Secret, also known as its secret API key.
    public required string ClientSecret { get; init; }
    /// The app's refresh token
    public required string RefreshToken { get; init; }
    /// The existing store access token
    public required string ExistingStoreAccessToken { get; init; }
}
#endif

public class ShopifyOauthUtility(IShopifyDomainUtility? domainUtility = null) : IShopifyOauthUtility
{
    private readonly IHttpClientFactory _httpClientFactory = new InternalHttpClientFactory();
    private readonly IShopifyDomainUtility _domainUtility = domainUtility ?? new ShopifyDomainUtility();

    /// <inheritdoc />
    public Uri BuildAuthorizationUrl(
        IEnumerable<AuthorizationScope> scopes,
        string shopDomain,
        string clientId,
        string redirectUrl,
        string? state = null,
        IEnumerable<string>? grants = null
    )
    {
        return BuildAuthorizationUrl(scopes.Select(s => s.ToSerializedString()), shopDomain, clientId, redirectUrl, state, grants);
    }

    /// <inheritdoc />
    public Uri BuildAuthorizationUrl(
        IEnumerable<string> scopes,
        string shopDomain,
        string clientId,
        string redirectUrl,
        string? state = null,
        IEnumerable<string>? grants = null
    )
    {
        grants = grants?.ToList();
        //Prepare a uri builder for the shop URL
        var builder = new UriBuilder(_domainUtility.BuildShopDomainUri(shopDomain));

        //Build the querystring
        var qs = new List<KeyValuePair<string, string>>()
        {
            new("client_id", clientId),
            new("scope", string.Join(",", scopes)),
            new("redirect_uri", redirectUrl),
        };

        if (!string.IsNullOrEmpty(state))
        {
            qs.Add(new KeyValuePair<string, string>("state", state));
        }

        if (grants?.Any() == true)
        {
            qs.AddRange(grants.Select(grant => new KeyValuePair<string, string>("grant_options[]", grant)));
        }

        builder.Path = "admin/oauth/authorize";
        builder.Query = string.Join("&", qs.Select(s => $"{s.Key}={s.Value}"));

        return builder.Uri;
    }

    #if NET8_0_OR_GREATER
    /// <inheritdoc />
    public Uri BuildAuthorizationUrl(AuthorizationUrlOptions options) =>
        BuildAuthorizationUrl(
            options.Scopes,
            options.ShopDomain,
            options.ClientId,
            options.RedirectUrl,
            options.State,
            options.Grants
        );
    #endif

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeAsync(
        string code,
        string shopDomain,
        string clientId,
        string clientSecret
    )
    {
        var ub = new UriBuilder(_domainUtility.BuildShopDomainUri(shopDomain))
        {
            Path = "admin/oauth/access_token"
        };
        var content = new JsonContent(new
        {
            client_id = clientId,
            client_secret = clientSecret,
            code,
        });

        var client = _httpClientFactory.CreateClient();
        using var request = new CloneableRequestMessage(ub.Uri, HttpMethod.Post, content);
        using var response = await client.SendAsync(request);
        var rawDataString = await response.Content.ReadAsStringAsync();

        ShopifyService.CheckResponseExceptions(response, rawDataString);

        var json = JToken.Parse(rawDataString);
        return new AuthorizationResult(json.Value<string>("access_token"), json.Value<string>("scope")?.Split(','));
    }

    #if NET8_0_OR_GREATER
    /// <inheritdoc />
    public Task<AuthorizationResult> AuthorizeAsync(AuthorizeOptions options) =>
        AuthorizeAsync(
            options.Code,
            options.ShopDomain,
            options.ClientId,
            options.ClientSecret
        );
    #endif

    /// <inheritdoc />
    public async Task<AuthorizationResult> RefreshAccessTokenAsync(
        string shopDomain,
        string clientId,
        string clientSecret,
        string refreshToken,
        string existingStoreAccessToken
    )
    {
        var ub = new UriBuilder(_domainUtility.BuildShopDomainUri(shopDomain))
        {
            Path = "admin/oauth/access_token"
        };
        var content = new JsonContent(new
        {
            client_id = clientId,
            client_secret = clientSecret,
            refresh_token = refreshToken,
            access_token = existingStoreAccessToken
        });

        var client = _httpClientFactory.CreateClient();
        using var request = new CloneableRequestMessage(ub.Uri, HttpMethod.Post, content);
        using var response = await client.SendAsync(request);
        var rawDataString = await response.Content.ReadAsStringAsync();

        ShopifyService.CheckResponseExceptions(response, rawDataString);

        var json = JToken.Parse(rawDataString);
        return new AuthorizationResult(json.Value<string>("access_token"), json.Value<string>("scope")?.Split(','));
    }

    #if NET8_0_OR_GREATER
    /// <inheritdoc />
    public Task<AuthorizationResult> RefreshAccessTokenAsync(RefreshAccessTokenOptions options) =>
        RefreshAccessTokenAsync(
            options.ShopDomain,
            options.ClientId,
            options.ClientSecret,
            options.RefreshToken,
            options.ExistingStoreAccessToken
        );
#endif
}
