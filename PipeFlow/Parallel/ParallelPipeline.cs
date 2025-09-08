using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PipeFlow.Core.Parallel;

public class ParallelPipeline<T> : IPipeline<T>
{
    private readonly IPipeline<T> _innerPipeline;
    private readonly int _maxDegreeOfParallelism;

    public ParallelPipeline(IPipeline<T> innerPipeline, int maxDegreeOfParallelism = -1)
    {
        _innerPipeline = innerPipeline ?? throw new ArgumentNullException(nameof(innerPipeline));
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    public IPipeline<T> Filter(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        var source = _innerPipeline.Execute();
        var parallelSource = source.AsParallel();
        if (_maxDegreeOfParallelism > 0)
        {
            parallelSource = parallelSource.WithDegreeOfParallelism(_maxDegreeOfParallelism);
        }
        
        return new Pipeline<T>(parallelSource.Where(predicate));
    }

    public IPipeline<T> Where(Func<T, bool> predicate)
    {
        return Filter(predicate);
    }

    public IPipeline<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        var source = _innerPipeline.Execute();
        var parallelSource = source.AsParallel();
        if (_maxDegreeOfParallelism > 0)
        {
            parallelSource = parallelSource.WithDegreeOfParallelism(_maxDegreeOfParallelism);
        }
        
        return new Pipeline<TResult>(parallelSource.Select(selector));
    }

    public IPipeline<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        return Map(selector);
    }

    public IPipeline<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector)
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        var source = _innerPipeline.Execute();
        var parallelSource = source.AsParallel();
        if (_maxDegreeOfParallelism > 0)
        {
            parallelSource = parallelSource.WithDegreeOfParallelism(_maxDegreeOfParallelism);
        }
        
        return new Pipeline<TResult>(parallelSource.SelectMany(selector));
    }

    public IPipeline<T> Take(int count)
    {
        return new Pipeline<T>(_innerPipeline.Take(count).Execute());
    }

    public IPipeline<T> Skip(int count)
    {
        return new Pipeline<T>(_innerPipeline.Skip(count).Execute());
    }

    public IPipeline<T> Distinct()
    {
        var source = _innerPipeline.Execute();
        var parallelSource = source.AsParallel();
        if (_maxDegreeOfParallelism > 0)
        {
            parallelSource = parallelSource.WithDegreeOfParallelism(_maxDegreeOfParallelism);
        }
        
        return new Pipeline<T>(parallelSource.Distinct());
    }

    public IPipeline<T> OrderBy<TKey>(Func<T, TKey> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        var source = _innerPipeline.Execute();
        var parallelSource = source.AsParallel();
        if (_maxDegreeOfParallelism > 0)
        {
            parallelSource = parallelSource.WithDegreeOfParallelism(_maxDegreeOfParallelism);
        }
        
        return new Pipeline<T>(parallelSource.OrderBy(keySelector));
    }

    public IPipeline<T> OrderByDescending<TKey>(Func<T, TKey> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        var source = _innerPipeline.Execute();
        var parallelSource = source.AsParallel();
        if (_maxDegreeOfParallelism > 0)
        {
            parallelSource = parallelSource.WithDegreeOfParallelism(_maxDegreeOfParallelism);
        }
        
        return new Pipeline<T>(parallelSource.OrderByDescending(keySelector));
    }

    public IEnumerable<T> Execute()
    {
        var source = _innerPipeline.Execute();
        if (_maxDegreeOfParallelism == 1)
        {
            return source;
        }
        
        var parallelSource = source.AsParallel();
        if (_maxDegreeOfParallelism > 0)
        {
            parallelSource = parallelSource.WithDegreeOfParallelism(_maxDegreeOfParallelism);
        }
        
        return parallelSource;
    }

    public async Task<IEnumerable<T>> ExecuteAsync()
    {
        return await Task.Run(() => Execute());
    }

    public void ForEach(Action<T> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var options = new ParallelOptions();
        if (_maxDegreeOfParallelism > 0)
        {
            options.MaxDegreeOfParallelism = _maxDegreeOfParallelism;
        }

        System.Threading.Tasks.Parallel.ForEach(Execute(), options, action);
    }

    public async Task ForEachAsync(Func<T, Task> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var semaphore = _maxDegreeOfParallelism > 0
            ? new System.Threading.SemaphoreSlim(_maxDegreeOfParallelism)
            : null;

        var tasks = Execute().Select(async item =>
        {
            if (semaphore != null)
                await semaphore.WaitAsync();

            try
            {
                await action(item);
            }
            finally
            {
                semaphore?.Release();
            }
        });

        await Task.WhenAll(tasks);
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