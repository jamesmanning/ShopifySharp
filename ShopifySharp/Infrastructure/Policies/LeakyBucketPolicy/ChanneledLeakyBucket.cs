#nullable enable
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ShopifySharp.Infrastructure.Policies.LeakyBucketPolicy;

public enum OverflowMode
{
    AddToQueue,
    DropNewest,
    DropOldest,
    ThrowException,
}

public class ChanneledLeakyBucket : IAsyncDisposable
{
    private readonly Channel<ChanneledLeakyBucketRequest> _channel;
    private readonly TimeProvider _timeProvider;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private int _currentTotalRequestsAvailable;
    private int _maximumRequestsPerSecond;
    private int _restoreRatePerSecond;
    private long _lastUpdatedTimestamp;

    public ChanneledLeakyBucket(
        int initialCurrentTotalRequestsAvailable,
        int maximumRequestsPerSecond,
        int restoreRatePerSecond,
        OverflowMode overflowMode = OverflowMode.AddToQueue,
        TimeProvider? timeProvider = null
    )
    {
        var t = new BoundedChannelOptions(maximumRequestsPerSecond)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        };
        _channel = Channel.CreateBounded<ChanneledLeakyBucketRequest>(maximumRequestsPerSecond);
        _currentTotalRequestsAvailable = initialCurrentTotalRequestsAvailable;
        _restoreRatePerSecond = restoreRatePerSecond;
        _maximumRequestsPerSecond = maximumRequestsPerSecond;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _lastUpdatedTimestamp = _timeProvider.GetTimestamp();
    }

    private int ComputeCurrentlyAvailable(TimeSpan timeSinceLastUpdate)
    {
        var secondsElapsed = timeSinceLastUpdate.TotalSeconds;
        var availableNow = _currentTotalRequestsAvailable + secondsElapsed * _restoreRatePerSecond;
        return (int) Math.Min(_maximumRequestsPerSecond, availableNow);
    }

    private TimeSpan? ComputeDelay(int requestCost, double availableNow)
    {
        var delayInSeconds = Math.Max(0, (requestCost - availableNow) / _restoreRatePerSecond);
        return delayInSeconds > 0 ? TimeSpan.FromSeconds(delayInSeconds) : null;
    }

    public ValueTask AddRequestAsync(ChanneledLeakyBucketRequest request, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(request, cancellationToken);
    }

    public void Stop()
    {
        _channel.Writer.Complete();
        _cancellationTokenSource.Cancel();
    }

    private async Task ProcessRequestAsync(ChanneledLeakyBucketRequest request, TimeSpan timeSinceLastUpdated, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || request.CancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            request.CancellationToken.ThrowIfCancellationRequested();
            // TODO: check if request is cancelled
        }

        if (_currentTotalRequestsAvailable - request.Cost < 0)
        {
            // TODO: put this request back in the channel at the front of the queue (somehow)
            await _channel.Writer.WriteAsync(request, cancellationToken);
            return;
        }

        Interlocked.Add(ref _currentTotalRequestsAvailable, -request.Cost);

        var delay = ComputeDelay(request.Cost, _currentTotalRequestsAvailable);

        if (delay is not null)
        {
            await Task.Delay(delay.Value);
            //await using var timer = _timeProvider.CreateTimer(_ =>
            //{

            //}, null, delay.Value, Timeout.InfiniteTimeSpan);
        }
        else
        {
            // TODO: await request here
        }
    }

    /// <remarks>
    /// The trick here is that this is being called continuously by the timer and does not need to schedule the next call.
    /// Therefore, this method can look at the current `_rate` value, burst all the way up to that rate, and trust that
    /// the requests will go through because this is the latest loop of burst requests and the `_rate` was just updated
    /// by the last timer tick.
    /// </remarks>
    private async Task ProcessQueueAsync()
    {
        var timestamp = _timeProvider.GetElapsedTime(_lastUpdatedTimestamp);
        var currentlyAvailable = ComputeCurrentlyAvailable(timestamp);

        Interlocked.Exchange(ref _currentTotalRequestsAvailable, currentlyAvailable);

        await Parallel.ForAsync(0, _currentTotalRequestsAvailable, _cancellationTokenSource.Token, async (_, cancellationToken) =>
        {
            try
            {
                if (_channel.Reader.TryRead(out var request))
                {
                    await ProcessRequestAsync(request, timestamp, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // TODO: figure out what to do during canceled exception messages
            }
            catch (Exception)
            {
                throw;
            }
        });
    }

    public void AdjustRate(int totalRequestsAvailable, int? restoreRatePerSecond = null, int? maximumRequestsPerSecond = null)
    {
        Interlocked.Add(ref _currentTotalRequestsAvailable, totalRequestsAvailable);
        if (restoreRatePerSecond.HasValue)
            Interlocked.Exchange(ref _restoreRatePerSecond, restoreRatePerSecond.Value);
        if (maximumRequestsPerSecond.HasValue)
            Interlocked.Exchange(ref _maximumRequestsPerSecond, maximumRequestsPerSecond.Value);
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await CastAndDispose(_cancellationTokenSource);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}

public class ChanneledLeakyBucketRequest(int cost, CancellationToken cancellationToken)
{
    public readonly int Cost = cost;
    public CancellationToken CancellationToken = cancellationToken;
}
