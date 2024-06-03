using ShopifySharp.Infrastructure.Policies.ExponentialRetry;

namespace ShopifySharp.Factories.Policies;

public class ExponentialRetryExecutionPolicyFactory(ExponentialRetryPolicyOptions options) : IRequestExecutionPolicyFactory
{
    public IRequestExecutionPolicy Create()
    {
        return new ExponentialRetryPolicy(options);
    }
}
