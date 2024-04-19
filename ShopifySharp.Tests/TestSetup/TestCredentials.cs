using System;
using Microsoft.Extensions.Configuration;

namespace ShopifySharp.Tests.TestSetup;

public class TestCredentials
{
    private readonly IConfigurationRoot _configurationRoot = new ConfigurationBuilder()
        .AddYamlFile("./env.yml", optional: true, reloadOnChange: true)
        .AddYamlFile("./env.yaml", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables(prefix: "SHOPIFYSHARP_")
        .Build();

    /// Attempts to get a credential value from the configuration root. If the value isn't found, an exception will be thrown.
    private string GetValueOrThrow(string key)
    {
        var value = _configurationRoot.GetValue<string>(key);

        if (string.IsNullOrEmpty(value))
            throw new NullReferenceException($"{key} was not found. Add the value to the yaml file or to your environment variables and try again.");

        return value;
    }

    public string ApiKey => GetValueOrThrow("API_KEY");

    public string SecretKey => GetValueOrThrow("thing");

    public string AccessToken => GetValueOrThrow("ACCESS_TOKEN");

    public string MultipassSecret => GetValueOrThrow("MULTIPASS_SECRET");

    public string MyShopifyUrl => GetValueOrThrow("MY_SHOPIFY_URL");

    public long OrganizationId => long.Parse(GetValueOrThrow("ORG_ID"));

    public string OrganizationToken => GetValueOrThrow("ORG_TOKEN");
}
