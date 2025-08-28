using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataFlow.Core;

public interface IPipeline<T>
{
    IPipeline<T> Filter(Func<T, bool> predicate);
    IPipeline<TResult> Map<TResult>(Func<T, TResult> selector);
    IPipeline<T> Where(Func<T, bool> predicate);
    IPipeline<TResult> Select<TResult>(Func<T, TResult> selector);
    IPipeline<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector);
    IPipeline<T> Take(int count);
    IPipeline<T> Skip(int count);
    IPipeline<T> Distinct();
    IPipeline<T> OrderBy<TKey>(Func<T, TKey> keySelector);
    IPipeline<T> OrderByDescending<TKey>(Func<T, TKey> keySelector);
    
    IEnumerable<T> Execute();
    Task<IEnumerable<T>> ExecuteAsync();
    
    void ForEach(Action<T> action);
    Task ForEachAsync(Func<T, Task> action);
    
    List<T> ToList();
    T[] ToArray();
    T First();
    T FirstOrDefault();
    int Count();
}