using System;

namespace ShopifySharp;

public interface IRequestExecutionPolicyRequiresOptions;

public interface IRequestExecutionPolicyRequiresOptions<out TOptions> : IRequestExecutionPolicyRequiresOptions
    where TOptions : class, IRequestExecutionPolicyOptions, new()
{
    static abstract Type GetOptionsType();
}
