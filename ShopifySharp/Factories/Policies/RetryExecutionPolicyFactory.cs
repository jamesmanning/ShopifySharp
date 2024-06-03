namespace ShopifySharp.Factories.Policies;

public class RetryExecutionPolicyFactory : IRequestExecutionPolicyFactory
{
    public IRequestExecutionPolicy Create()
    {
        return new RetryExecutionPolicy();
    }
}
