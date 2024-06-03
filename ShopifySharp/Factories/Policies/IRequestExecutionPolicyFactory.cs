namespace ShopifySharp.Factories.Policies;

public interface IRequestExecutionPolicyFactory;

public interface IRequestExecutionPolicyFactory<out TPolicy> : IRequestExecutionPolicyFactory
    where TPolicy : class, IRequestExecutionPolicy
{
    TPolicy Create();
}
