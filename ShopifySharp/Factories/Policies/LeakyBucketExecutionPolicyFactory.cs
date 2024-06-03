namespace ShopifySharp.Factories.Policies;

public class LeakyBucketExecutionPolicyFactory : IRequestExecutionPolicyFactory
{
    public IRequestExecutionPolicy Create()
    {
        return new LeakyBucketExecutionPolicy();
    }
}
