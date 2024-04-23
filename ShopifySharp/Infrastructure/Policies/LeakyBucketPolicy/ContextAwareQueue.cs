#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
        var context = _getContext?.Invoke();

        switch (context)
        {
            case RequestContext.Background:
                _backgroundQueue.Enqueue(i);
                break;
            case RequestContext.Foreground:
                _foregroundQueue.Enqueue(i);
                break;
            case null:
                // If no context function is set, default to queuing on the foreground
                _foregroundQueue.Enqueue(i);
                break;
            default:
                throw new SwitchExpressionException(context);
        }
    }

    public T Peek() => _foregroundQueue.Count > 0 ? _foregroundQueue.Peek() : _backgroundQueue.Peek();

    public T Dequeue() => _foregroundQueue.Count > 0 ? _foregroundQueue.Dequeue() : _backgroundQueue.Dequeue();
}
