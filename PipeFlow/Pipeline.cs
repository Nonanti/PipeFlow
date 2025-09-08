using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PipeFlow.Core;

public class Pipeline<T> : IPipeline<T>
{
    private readonly IEnumerable<T> _source;
    private readonly Func<IEnumerable<T>, IEnumerable<T>> _operation;

    public Pipeline(IEnumerable<T> source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _operation = null;
    }

    private Pipeline(IEnumerable<T> source, Func<IEnumerable<T>, IEnumerable<T>> operation)
    {
        _source = source;
        _operation = operation;
    }

    public IPipeline<T> Filter(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        
        var newOperation = CreateOperation(data => data.Where(predicate));
        return new Pipeline<T>(_source, newOperation);
    }

    public IPipeline<T> Where(Func<T, bool> predicate)
    {
        return Filter(predicate);
    }

    public IPipeline<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));
        
        var transformedData = GetSource().Select(selector);
        return new Pipeline<TResult>(transformedData);
    }

    public IPipeline<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        return Map(selector);
    }

    public IPipeline<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector)
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));
        
        var flattened = GetSource().SelectMany(selector);
        return new Pipeline<TResult>(flattened);
    }

    public IPipeline<T> Take(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
        
        var newOperation = CreateOperation(data => data.Take(count));
        return new Pipeline<T>(_source, newOperation);
    }

    public IPipeline<T> Skip(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
        
        var newOperation = CreateOperation(data => data.Skip(count));
        return new Pipeline<T>(_source, newOperation);
    }

    public IPipeline<T> Distinct()
    {
        var newOperation = CreateOperation(data => data.Distinct());
        return new Pipeline<T>(_source, newOperation);
    }

    public IPipeline<T> OrderBy<TKey>(Func<T, TKey> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        
        var newOperation = CreateOperation(data => data.OrderBy(keySelector));
        return new Pipeline<T>(_source, newOperation);
    }

    public IPipeline<T> OrderByDescending<TKey>(Func<T, TKey> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        
        var newOperation = CreateOperation(data => data.OrderByDescending(keySelector));
        return new Pipeline<T>(_source, newOperation);
    }

    public IEnumerable<T> Execute()
    {
        return GetSource();
    }

    private IEnumerable<T> GetSource()
    {
        if (_operation == null)
        {
            return _source;
        }
        else
        {
            return _operation(_source);
        }
    }

    private Func<IEnumerable<T>, IEnumerable<T>> CreateOperation(Func<IEnumerable<T>, IEnumerable<T>> newOp)
    {
        if (_operation == null)
        {
            return newOp;
        }
        else
        {
            return data => newOp(_operation(data));
        }
    }

    public async Task<IEnumerable<T>> ExecuteAsync()
    {
        return await Task.Run(() => Execute());
    }

    public void ForEach(Action<T> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        
        foreach (var item in GetSource())
        {
            action(item);
        }
    }

    public async Task ForEachAsync(Func<T, Task> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        
        foreach (var item in GetSource())
        {
            await action(item);
        }
    }

    public List<T> ToList()
    {
        return GetSource().ToList();
    }

    public T[] ToArray()
    {
        return GetSource().ToArray();
    }

    public T First()
    {
        return GetSource().First();
    }

    public T FirstOrDefault()
    {
        return GetSource().FirstOrDefault();
    }

    public int Count()
    {
        return GetSource().Count();
    }
}