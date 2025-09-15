using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PipeFlow.Core.Builder;

public class QueryablePipelineBuilder<T> : IPipelineBuilder<T> where T : class
{
  private IQueryable<T> _queryable;
  private int _pageSize = 1000;
  private bool _isParallel = false;
  private int _maxDegreeOfParallelism = -1;

  public QueryablePipelineBuilder(IQueryable<T> queryable)
  {
    _queryable = queryable ?? throw new ArgumentNullException(nameof(queryable));
  }

  public IPipelineBuilder<T> Filter(Func<T, bool> predicate)
  {
    if (predicate == null)
      throw new ArgumentNullException(nameof(predicate));
    
    _queryable = _queryable.Where(predicate).AsQueryable();
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
    
    var transformed = _queryable.Select(selector);
    return new PipelineBuilder<TResult>(transformed);
  }

  public IPipelineBuilder<TResult> Select<TResult>(Func<T, TResult> selector)
  {
    return Map(selector);
  }

  public IPipelineBuilder<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector)
  {
    if (selector == null)
      throw new ArgumentNullException(nameof(selector));
    
    var flattened = _queryable.SelectMany(selector);
    return new PipelineBuilder<TResult>(flattened);
  }

  public IPipelineBuilder<T> Take(int count)
  {
    if (count < 0)
      throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
    
    _queryable = _queryable.Take(count);
    return this;
  }

  public IPipelineBuilder<T> Skip(int count)
  {
    if (count < 0)
      throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
    
    _queryable = _queryable.Skip(count);
    return this;
  }

  public IPipelineBuilder<T> Distinct()
  {
    _queryable = _queryable.Distinct();
    return this;
  }

  public IPipelineBuilder<T> OrderBy<TKey>(Func<T, TKey> keySelector)
  {
    if (keySelector == null)
      throw new ArgumentNullException(nameof(keySelector));
    
    _queryable = _queryable.OrderBy(keySelector).AsQueryable();
    return this;
  }

  public IPipelineBuilder<T> OrderByDescending<TKey>(Func<T, TKey> keySelector)
  {
    if (keySelector == null)
      throw new ArgumentNullException(nameof(keySelector));
    
    _queryable = _queryable.OrderByDescending(keySelector).AsQueryable();
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
    
    _pageSize = batchSize;
    return this;
  }
  
  public IPipelineBuilder<T> WithPaging(int pageSize = 1000)
  {
    if (pageSize <= 0)
      throw new ArgumentException("Page size must be greater than zero", nameof(pageSize));
    
    _pageSize = pageSize;
    return this;
  }

  public IExecutablePipeline<T> Build()
  {
    return new QueryableExecutablePipeline<T>(_queryable, _pageSize, _isParallel, _maxDegreeOfParallelism);
  }
}

public class QueryableExecutablePipeline<T> : IExecutablePipeline<T> where T : class
{
  private readonly IQueryable<T> _queryable;
  private readonly int _pageSize;
  private readonly bool _isParallel;
  private readonly int _maxDegreeOfParallelism;

  public QueryableExecutablePipeline(
    IQueryable<T> queryable, 
    int pageSize,
    bool isParallel,
    int maxDegreeOfParallelism)
  {
    _queryable = queryable;
    _pageSize = pageSize;
    _isParallel = isParallel;
    _maxDegreeOfParallelism = maxDegreeOfParallelism;
  }

  public PipelineResult<T> Execute()
  {
    try
    {
      var data = GetPagedData().ToList();
      return PipelineResult<T>.SuccessResult(data, data.Count);
    }
    catch (Exception ex)
    {
      return PipelineResult<T>.FailureResult(new List<string> { ex.Message });
    }
  }

  public async Task<PipelineResult<T>> ExecuteAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      var data = new List<T>();
      await foreach (var item in GetPagedDataAsync(cancellationToken))
      {
        data.Add(item);
      }
      return PipelineResult<T>.SuccessResult(data, data.Count);
    }
    catch (Exception ex)
    {
      return PipelineResult<T>.FailureResult(new List<string> { ex.Message });
    }
  }

  public async IAsyncEnumerable<T> StreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    await foreach (var item in GetPagedDataAsync(cancellationToken))
    {
      yield return item;
    }
  }

  public IEnumerable<T> Stream()
  {
    return GetPagedData();
  }

  public void ForEach(Action<T> action)
  {
    if (action == null)
      throw new ArgumentNullException(nameof(action));
    
    foreach (var item in GetPagedData())
    {
      action(item);
    }
  }

  public async Task ForEachAsync(Func<T, Task> action, CancellationToken cancellationToken = default)
  {
    if (action == null)
      throw new ArgumentNullException(nameof(action));
    
    await foreach (var item in GetPagedDataAsync(cancellationToken))
    {
      await action(item);
    }
  }

  public List<T> ToList()
  {
    return GetPagedData().ToList();
  }

  public async Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
  {
    var result = new List<T>();
    await foreach (var item in GetPagedDataAsync(cancellationToken))
    {
      result.Add(item);
    }
    return result;
  }

  public T[] ToArray()
  {
    return GetPagedData().ToArray();
  }

  public async Task<T[]> ToArrayAsync(CancellationToken cancellationToken = default)
  {
    var list = await ToListAsync(cancellationToken);
    return list.ToArray();
  }

  public T First()
  {
    return _queryable.First();
  }

  public async Task<T> FirstAsync(CancellationToken cancellationToken = default)
  {
    if (IsEntityFrameworkQueryable())
    {
      return await EntityFrameworkQueryableExtensions.FirstAsync(_queryable, cancellationToken);
    }
    return await Task.Run(() => _queryable.First(), cancellationToken);
  }

  public T FirstOrDefault()
  {
    return _queryable.FirstOrDefault();
  }

  public async Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
  {
    if (IsEntityFrameworkQueryable())
    {
      return await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_queryable, cancellationToken);
    }
    return await Task.Run(() => _queryable.FirstOrDefault(), cancellationToken);
  }

  public int Count()
  {
    return _queryable.Count();
  }

  public async Task<int> CountAsync(CancellationToken cancellationToken = default)
  {
    if (IsEntityFrameworkQueryable())
    {
      return await EntityFrameworkQueryableExtensions.CountAsync(_queryable, cancellationToken);
    }
    return await Task.Run(() => _queryable.Count(), cancellationToken);
  }

  private IEnumerable<T> GetPagedData()
  {
    var skip = 0;
    var hasMore = true;
    
    while (hasMore)
    {
      var batch = _queryable.Skip(skip).Take(_pageSize).ToList();
      
      if (batch.Count == 0)
      {
        hasMore = false;
      }
      else
      {
        foreach (var item in batch)
        {
          yield return item;
        }
        
        if (batch.Count < _pageSize)
        {
          hasMore = false;
        }
        
        skip += _pageSize;
      }
    }
  }

  private async IAsyncEnumerable<T> GetPagedDataAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var skip = 0;
    var hasMore = true;
    
    while (hasMore && !cancellationToken.IsCancellationRequested)
    {
      List<T> batch;
      
      if (IsEntityFrameworkQueryable())
      {
        batch = await EntityFrameworkQueryableExtensions.ToListAsync(
          _queryable.Skip(skip).Take(_pageSize), 
          cancellationToken);
      }
      else
      {
        batch = await Task.Run(() => _queryable.Skip(skip).Take(_pageSize).ToList(), cancellationToken);
      }
      
      if (batch.Count == 0)
      {
        hasMore = false;
      }
      else
      {
        foreach (var item in batch)
        {
          yield return item;
        }
        
        if (batch.Count < _pageSize)
        {
          hasMore = false;
        }
        
        skip += _pageSize;
      }
    }
  }

  private bool IsEntityFrameworkQueryable()
  {
    return _queryable.Provider.GetType().FullName?.Contains("EntityFramework") == true;
  }
}