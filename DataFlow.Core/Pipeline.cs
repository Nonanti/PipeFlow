using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataFlow.Core;

public class Pipeline<T> : IPipeline<T>
{
    private readonly IEnumerable<T> _source;
    private readonly List<Func<IEnumerable<T>, IEnumerable<T>>> _operations;

    public Pipeline(IEnumerable<T> source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _operations = new List<Func<IEnumerable<T>, IEnumerable<T>>>();
    }

    private Pipeline(IEnumerable<T> source, List<Func<IEnumerable<T>, IEnumerable<T>>> operations)
    {
        _source = source;
        _operations = new List<Func<IEnumerable<T>, IEnumerable<T>>>(operations);
    }

    public IPipeline<T> Filter(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        
        var newPipeline = new Pipeline<T>(_source, _operations);
        newPipeline._operations.Add(data => data.Where(predicate));
        return newPipeline;
    }

    public IPipeline<T> Where(Func<T, bool> predicate)
    {
        return Filter(predicate);
    }

    public IPipeline<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));
        
        var transformedData = Execute().Select(selector);
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
        
        var flattened = Execute().SelectMany(selector);
        return new Pipeline<TResult>(flattened);
    }

    public IPipeline<T> Take(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
        
        var newPipeline = new Pipeline<T>(_source, _operations);
        newPipeline._operations.Add(data => data.Take(count));
        return newPipeline;
    }

    public IPipeline<T> Skip(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
        
        var newPipeline = new Pipeline<T>(_source, _operations);
        newPipeline._operations.Add(data => data.Skip(count));
        return newPipeline;
    }

    public IPipeline<T> Distinct()
    {
        var newPipeline = new Pipeline<T>(_source, _operations);
        newPipeline._operations.Add(data => data.Distinct());
        return newPipeline;
    }

    public IPipeline<T> OrderBy<TKey>(Func<T, TKey> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        
        var newPipeline = new Pipeline<T>(_source, _operations);
        newPipeline._operations.Add(data => data.OrderBy(keySelector));
        return newPipeline;
    }

    public IPipeline<T> OrderByDescending<TKey>(Func<T, TKey> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        
        var newPipeline = new Pipeline<T>(_source, _operations);
        newPipeline._operations.Add(data => data.OrderByDescending(keySelector));
        return newPipeline;
    }

    public IEnumerable<T> Execute()
    {
        IEnumerable<T> result = _source;
        
        foreach (var operation in _operations)
        {
            result = operation(result);
        }
        
        return result;
    }

    public async Task<IEnumerable<T>> ExecuteAsync()
    {
        return await Task.Run(() => Execute());
    }

    public void ForEach(Action<T> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        
        foreach (var item in Execute())
        {
            action(item);
        }
    }

    public async Task ForEachAsync(Func<T, Task> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        
        foreach (var item in Execute())
        {
            await action(item);
        }
    }

    public List<T> ToList()
    {
        return Execute().ToList();
    }

    public T[] ToArray()
    {
        return Execute().ToArray();
    }

    public T First()
    {
        return Execute().First();
    }

    public T FirstOrDefault()
    {
        return Execute().FirstOrDefault();
    }

    public int Count()
    {
        return Execute().Count();
    }
}