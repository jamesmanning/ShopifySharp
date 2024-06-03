namespace ShopifySharp.Factories.Policies;

public class DefaultExecutionPolicyFactory : IRequestExecutionPolicyFactory
{
    public IRequestExecutionPolicy Create()
    {
        return new DefaultRequestExecutionPolicy();
    }
}
