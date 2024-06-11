namespace ShopifySharp.Infrastructure.Policies;

public interface IRequestExecutionPolicyRequiresOptions;

public interface IRequestExecutionPolicyRequiresOptions<out TOptions> : IRequestExecutionPolicyRequiresOptions
    where TOptions : class, IRequestExecutionPolicyOptions<TOptions>, new();
