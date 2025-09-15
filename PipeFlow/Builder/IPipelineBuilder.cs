using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PipeFlow.Core.Builder;

public interface IPipelineBuilder<T>
{
  IPipelineBuilder<T> Filter(Func<T, bool> predicate);
  IPipelineBuilder<TResult> Map<TResult>(Func<T, TResult> selector);
  IPipelineBuilder<T> Where(Func<T, bool> predicate);
  IPipelineBuilder<TResult> Select<TResult>(Func<T, TResult> selector);
  IPipelineBuilder<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector);
  IPipelineBuilder<T> Take(int count);
  IPipelineBuilder<T> Skip(int count);
  IPipelineBuilder<T> Distinct();
  IPipelineBuilder<T> OrderBy<TKey>(Func<T, TKey> keySelector);
  IPipelineBuilder<T> OrderByDescending<TKey>(Func<T, TKey> keySelector);
  
  IPipelineBuilder<T> AsParallel(int maxDegreeOfParallelism = -1);
  IPipelineBuilder<T> WithBatchSize(int batchSize);
  
  IExecutablePipeline<T> Build();
}

public interface IExecutablePipeline<T>
{
  PipelineResult<T> Execute();
  Task<PipelineResult<T>> ExecuteAsync(CancellationToken cancellationToken = default);
  
  IAsyncEnumerable<T> StreamAsync(CancellationToken cancellationToken = default);
  IEnumerable<T> Stream();
  
  void ForEach(Action<T> action);
  Task ForEachAsync(Func<T, Task> action, CancellationToken cancellationToken = default);
  
  List<T> ToList();
  Task<List<T>> ToListAsync(CancellationToken cancellationToken = default);
  
  T[] ToArray();
  Task<T[]> ToArrayAsync(CancellationToken cancellationToken = default);
  
  T First();
  Task<T> FirstAsync(CancellationToken cancellationToken = default);
  
  T FirstOrDefault();
  Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
  
  int Count();
  Task<int> CountAsync(CancellationToken cancellationToken = default);
}

public class PipelineResult<T>
{
  public bool Success { get; set; }
  public IEnumerable<T> Data { get; set; }
  public List<string> Errors { get; set; } = new List<string>();
  public int ProcessedCount { get; set; }
  public int FailedCount { get; set; }
  public TimeSpan ExecutionTime { get; set; }
  
  public static PipelineResult<T> SuccessResult(IEnumerable<T> data, int processedCount = 0, TimeSpan? executionTime = null)
  {
    return new PipelineResult<T>
    {
      Success = true,
      Data = data,
      ProcessedCount = processedCount,
      ExecutionTime = executionTime ?? TimeSpan.Zero
    };
  }
  
  public static PipelineResult<T> FailureResult(List<string> errors)
  {
    return new PipelineResult<T>
    {
      Success = false,
      Errors = errors
    };
  }
}