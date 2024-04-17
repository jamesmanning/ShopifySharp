namespace ShopifySharp.Infrastructure.Policies.LeakyBucketPolicy;

internal enum ApiType : byte
{
    RESTAdmin,

    GraphQLAdmin,

    GraphQLPartner,
}
