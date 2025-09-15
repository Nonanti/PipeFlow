using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PipeFlow.Core.Builder;

public class PipelineBuilder<T> : IPipelineBuilder<T>
{
  private readonly IEnumerable<T> _source;
  private readonly List<Func<IEnumerable<T>, IEnumerable<T>>> _operations;
  private int _batchSize = 1000;
  private int _maxDegreeOfParallelism = -1;
  private bool _isParallel = false;

  public PipelineBuilder(IEnumerable<T> source)
  {
    _source = source ?? throw new ArgumentNullException(nameof(source));
    _operations = new List<Func<IEnumerable<T>, IEnumerable<T>>>();
  }

  public IPipelineBuilder<T> Filter(Func<T, bool> predicate)
  {
    if (predicate == null)
      throw new ArgumentNullException(nameof(predicate));
    
    _operations.Add(data => data.Where(predicate));
    return this;
  }

  public IPipelineBuilder<T> Where(Func<T, bool> predicate)
  {
    return Filter(predicate);
  }

  public IPipelineBuilder<TResult> Map<TResult>(Func<T, TResult> selector)
  {
    if (selector == null)
      throw new ArgumentNullException(nameof(selector));
    
    var transformedSource = ApplyOperations().Select(selector);
    return new PipelineBuilder<TResult>(transformedSource);
  }

  public IPipelineBuilder<TResult> Select<TResult>(Func<T, TResult> selector)
  {
    return Map(selector);
  }

  public IPipelineBuilder<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector)
  {
    if (selector == null)
      throw new ArgumentNullException(nameof(selector));
    
    var flattened = ApplyOperations().SelectMany(selector);
    return new PipelineBuilder<TResult>(flattened);
  }

  public IPipelineBuilder<T> Take(int count)
  {
    if (count < 0)
      throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
    
    _operations.Add(data => data.Take(count));
    return this;
  }

  public IPipelineBuilder<T> Skip(int count)
  {
    if (count < 0)
      throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
    
    _operations.Add(data => data.Skip(count));
    return this;
  }

  public IPipelineBuilder<T> Distinct()
  {
    _operations.Add(data => data.Distinct());
    return this;
  }

  public IPipelineBuilder<T> OrderBy<TKey>(Func<T, TKey> keySelector)
  {
    if (keySelector == null)
      throw new ArgumentNullException(nameof(keySelector));
    
    _operations.Add(data => data.OrderBy(keySelector));
    return this;
  }

  public IPipelineBuilder<T> OrderByDescending<TKey>(Func<T, TKey> keySelector)
  {
    if (keySelector == null)
      throw new ArgumentNullException(nameof(keySelector));
    
    _operations.Add(data => data.OrderByDescending(keySelector));
    return this;
  }

  public IPipelineBuilder<T> AsParallel(int maxDegreeOfParallelism = -1)
  {
    _isParallel = true;
    _maxDegreeOfParallelism = maxDegreeOfParallelism;
    return this;
  }

  public IPipelineBuilder<T> WithBatchSize(int batchSize)
  {
    if (batchSize <= 0)
      throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));
    
    _batchSize = batchSize;
    return this;
  }

  public IExecutablePipeline<T> Build()
  {
    return new ExecutablePipeline<T>(
      _source, 
      _operations, 
      _isParallel, 
      _maxDegreeOfParallelism, 
      _batchSize);
  }

  private IEnumerable<T> ApplyOperations()
  {
    var result = _source;
    foreach (var operation in _operations)
    {
      result = operation(result);
    }
    return result;
  }
}

public class ExecutablePipeline<T> : IExecutablePipeline<T>
{
  private readonly IEnumerable<T> _source;
  private readonly List<Func<IEnumerable<T>, IEnumerable<T>>> _operations;
  private readonly bool _isParallel;
  private readonly int _maxDegreeOfParallelism;
  private readonly int _batchSize;

  public ExecutablePipeline(
    IEnumerable<T> source, 
    List<Func<IEnumerable<T>, IEnumerable<T>>> operations,
    bool isParallel,
    int maxDegreeOfParallelism,
    int batchSize)
  {
    _source = source;
    _operations = operations;
    _isParallel = isParallel;
    _maxDegreeOfParallelism = maxDegreeOfParallelism;
    _batchSize = batchSize;
  }

  public PipelineResult<T> Execute()
  {
    var stopwatch = Stopwatch.StartNew();
    try
    {
      var result = ApplyOperations();
      var data = result.ToList();
      stopwatch.Stop();
      
      return PipelineResult<T>.SuccessResult(data, data.Count, stopwatch.Elapsed);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      return PipelineResult<T>.FailureResult(new List<string> { ex.Message });
    }
  }

  public async Task<PipelineResult<T>> ExecuteAsync(CancellationToken cancellationToken = default)
  {
    var stopwatch = Stopwatch.StartNew();
    try
    {
      var result = await Task.Run(() => ApplyOperations(), cancellationToken);
      var data = result.ToList();
      stopwatch.Stop();
      
      return PipelineResult<T>.SuccessResult(data, data.Count, stopwatch.Elapsed);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      return PipelineResult<T>.FailureResult(new List<string> { ex.Message });
    }
  }

  public async IAsyncEnumerable<T> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    await foreach (var item in Task.Run(() => ApplyOperations(), cancellationToken).ToAsyncEnumerable())
    {
      if (cancellationToken.IsCancellationRequested)
        yield break;
      
      yield return item;
    }
  }

  public IEnumerable<T> Stream()
  {
    return ApplyOperations();
  }

  public void ForEach(Action<T> action)
  {
    if (action == null)
      throw new ArgumentNullException(nameof(action));
    
    foreach (var item in ApplyOperations())
    {
      action(item);
    }
  }

  public async Task ForEachAsync(Func<T, Task> action, CancellationToken cancellationToken = default)
  {
    if (action == null)
      throw new ArgumentNullException(nameof(action));
    
    foreach (var item in ApplyOperations())
    {
      if (cancellationToken.IsCancellationRequested)
        break;
      
      await action(item);
    }
  }

  public List<T> ToList()
  {
    return ApplyOperations().ToList();
  }

  public async Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
  {
    return await Task.Run(() => ApplyOperations().ToList(), cancellationToken);
  }

  public T[] ToArray()
  {
    return ApplyOperations().ToArray();
  }

  public async Task<T[]> ToArrayAsync(CancellationToken cancellationToken = default)
  {
    return await Task.Run(() => ApplyOperations().ToArray(), cancellationToken);
  }

  public T First()
  {
    return ApplyOperations().First();
  }

  public async Task<T> FirstAsync(CancellationToken cancellationToken = default)
  {
    return await Task.Run(() => ApplyOperations().First(), cancellationToken);
  }

  public T FirstOrDefault()
  {
    return ApplyOperations().FirstOrDefault();
  }

  public async Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
  {
    return await Task.Run(() => ApplyOperations().FirstOrDefault(), cancellationToken);
  }

  public int Count()
  {
    return ApplyOperations().Count();
  }

  public async Task<int> CountAsync(CancellationToken cancellationToken = default)
  {
    return await Task.Run(() => ApplyOperations().Count(), cancellationToken);
  }

  private IEnumerable<T> ApplyOperations()
  {
    var result = _source;
    
    if (_isParallel)
    {
      var parallelQuery = result.AsParallel();
      if (_maxDegreeOfParallelism > 0)
      {
        parallelQuery = parallelQuery.WithDegreeOfParallelism(_maxDegreeOfParallelism);
      }
      result = parallelQuery;
    }
    
    foreach (var operation in _operations)
    {
      result = operation(result);
    }
    
    return result;
  }
}

internal static class AsyncEnumerableExtensions
{
  public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this Task<IEnumerable<T>> task)
  {
    var items = await task;
    foreach (var item in items)
    {
      yield return item;
    }
  }
}