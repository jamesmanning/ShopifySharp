namespace ShopifySharp;

public interface IRequestExecutionPolicyOptions
{
    void Validate();
}

public interface IRequestExecutionPolicyOptions<out TOptions> : IRequestExecutionPolicyOptions
    where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new()
{
    static abstract TOptions Default();
}
