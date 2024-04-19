using System;
using System.Threading;

namespace ShopifySharp.Infrastructure.Policies.LeakyBucketPolicy;

internal class LeakyBucketPendingRequest(int cost, CancellationToken cancellationToken) : IDisposable
{
    public int Cost = cost;
    public readonly SemaphoreSlim Semaphore = new (0, 1);
    public CancellationToken CancellationToken = cancellationToken;

    public void Dispose()
    {
        Semaphore?.Dispose();
    }
}
