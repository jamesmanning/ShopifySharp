#nullable enable
using System;
using System.Collections.Generic;

namespace ShopifySharp.Infrastructure.Policies.LeakyBucketPolicy;

internal class ContextAwareQueue<T>()
{
    private readonly Queue<T> _backgroundQueue = new Queue<T>();

    private readonly Queue<T> _foregroundQueue = new Queue<T>();

    private readonly Func<RequestContext>? _getContext;

    public ContextAwareQueue(Func<RequestContext>? getContext = null) : this()
    {
        _getContext = getContext;
    }

    public int Count => _backgroundQueue.Count + _foregroundQueue.Count;

    public void Enqueue(T i)
    {
        var context = _getContext?.Invoke() ?? RequestContext.Foreground;
        (context == RequestContext.Background ? _backgroundQueue : _foregroundQueue).Enqueue(i);
    }

    public T Peek() => _foregroundQueue.Count > 0 ? _foregroundQueue.Peek() : _backgroundQueue.Peek();

    public T Dequeue() => _foregroundQueue.Count > 0 ? _foregroundQueue.Dequeue() : _backgroundQueue.Dequeue();
}
