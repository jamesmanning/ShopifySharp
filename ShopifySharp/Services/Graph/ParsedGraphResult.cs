#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using ShopifySharp.GraphQL;

namespace ShopifySharp;

[Serializable]
public class ParsedGraphResult<T>
{
    [JsonProperty("data"), JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonProperty("userErrors"), JsonPropertyName("userErrors")]
    public ICollection<UserError>? UserErrors { get; set; }

    public GraphExtensions? Extensions { get; set; }
}
