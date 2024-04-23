using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using ShopifySharp.Infrastructure.Policies.LeakyBucketPolicy;
using Xunit;

namespace ShopifySharp.Tests.Policies;

public class LeakyBucketTests
{
    private readonly ContextAwareQueue<LeakyBucketPendingRequest> _leakyBucketRequestQueue = Substitute.For<ContextAwareQueue<LeakyBucketPendingRequest>>();

    [Fact]
    public async Task WaitForAvailableAsync_ShouldDequeueRequestAfterAwaitingIt()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var bucket = new LeakyBucket(10, 1, () => now);
        var cts = new CancellationTokenSource();

        // Act
        await bucket.WaitForAvailableAsync(1, cts.Token);
        var result = bucket.PendingRequests;

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void WaitForAvailableAsync_ShouldDequeueRequestIfItThrows()
    {
        throw new NotImplementedException();
    }
}
