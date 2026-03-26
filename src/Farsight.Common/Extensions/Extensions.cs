using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Farsight.Common.Extensions;

/// <summary>
/// Common extensions used in Farsight applications.
/// </summary>
public static class Extensions
{
    extension<T>(ReadOnlyMemory<T> v)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<T> AsEnumerable() => MemoryMarshal.ToEnumerable(v);

        /// <summary>
        /// Returns an enumerator for this <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpan<T>.Enumerator GetEnumerator()
            => v.Span.GetEnumerator();

        /// <summary>
        /// Determines whether the memory contains any elements.
        /// </summary>
        /// <returns><see langword="true"/> if the memory contains at least one element; otherwise, <see langword="false"/>.</returns>
        public bool Any() => v.Length > 0;

        /// <summary>
        /// Determines whether any element in the memory satisfies the specified predicate.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns><see langword="true"/> if any element matches the predicate; otherwise, <see langword="false"/>.</returns>
        public bool Any(Func<T, bool> predicate) => v.AsEnumerable().Any(predicate);

        /// <summary>
        /// Determines whether all elements in the memory satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns><see langword="true"/> if every element matches the predicate; otherwise, <see langword="false"/>.</returns>
        public bool All(Func<T, bool> predicate) => v.AsEnumerable().All(predicate);

        /// <summary>
        /// Filters the elements of the memory based on the specified predicate.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns>A sequence that contains the elements that satisfy the predicate.</returns>
        public IEnumerable<T> Where(Func<T, bool> predicate) => v.AsEnumerable().Where(predicate);

        /// <summary>
        /// Projects each element of the memory into a new form.
        /// </summary>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
        /// <param name="selector">The transform function to apply to each element.</param>
        /// <returns>A sequence whose elements are the result of invoking the transform function on each element.</returns>
        public IEnumerable<TResult> Select<TResult>(Func<T, TResult> selector) => v.AsEnumerable().Select(selector);

        /// <summary>
        /// Projects each element of the memory into a sequence and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by <paramref name="selector"/>.</typeparam>
        /// <param name="selector">The transform function that returns a sequence for each element.</param>
        /// <returns>A sequence whose elements are the concatenated results of invoking the transform function on each element.</returns>
        public IEnumerable<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector) => v.AsEnumerable().SelectMany(selector);

        /// <summary>
        /// Creates a <see cref="List{T}"/> from the elements in the memory.
        /// </summary>
        /// <returns>A list that contains the elements of the memory.</returns>
        public List<T> ToList() => [.. v.AsEnumerable()];

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from the elements in the memory according to a specified key selector.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract a key from each element.</param>
        /// <returns>A dictionary that contains keys and values derived from the memory.</returns>
        public Dictionary<TKey, T> ToDictionary<TKey>(Func<T, TKey> keySelector) where TKey : notnull
            => v.AsEnumerable().ToDictionary(keySelector);

        /// <summary>
        /// Applies an accumulator function over the elements of the memory.
        /// </summary>
        /// <param name="func">The accumulator function to apply.</param>
        /// <returns>The final accumulated value.</returns>
        public T Aggregate(Func<T, T, T> func) => v.AsEnumerable().Aggregate(func);

        /// <summary>
        /// Applies an accumulator function over the elements of the memory using the specified seed value and result selector.
        /// </summary>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">The accumulator function to apply to each element.</param>
        /// <param name="resultSelector">The transform function used to produce the final result.</param>
        /// <returns>The transformed final accumulator value.</returns>
        public TResult Aggregate<TAccumulate, TResult>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
            => v.AsEnumerable().Aggregate(seed, func, resultSelector);

        /// <summary>
        /// Sorts the elements of the memory in ascending order according to a key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract a sort key from each element.</param>
        /// <returns>An ordered sequence of the memory elements.</returns>
        public IOrderedEnumerable<T> OrderBy<TKey>(Func<T, TKey> keySelector) => v.AsEnumerable().OrderBy(keySelector);

        /// <summary>
        /// Sorts the elements of the memory in descending order according to a key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract a sort key from each element.</param>
        /// <returns>An ordered sequence of the memory elements.</returns>
        public IOrderedEnumerable<T> OrderByDescending<TKey>(Func<T, TKey> keySelector) => v.AsEnumerable().OrderByDescending(keySelector);

        /// <summary>
        /// Groups the elements of the memory according to a specified key selector.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract the key for each element.</param>
        /// <returns>A sequence of groups whose keys are produced by the key selector.</returns>
        public IEnumerable<IGrouping<TKey, T>> GroupBy<TKey>(Func<T, TKey> keySelector) => v.AsEnumerable().GroupBy(keySelector);

        /// <summary>
        /// Returns distinct elements from the memory.
        /// </summary>
        /// <returns>A sequence that contains distinct elements from the memory.</returns>
        public IEnumerable<T> Distinct() => v.AsEnumerable().Distinct();

        /// <summary>
        /// Concatenates the memory with another sequence.
        /// </summary>
        /// <param name="second">The sequence to concatenate to the memory.</param>
        /// <returns>A sequence that contains the elements of the memory followed by the elements of <paramref name="second"/>.</returns>
        public IEnumerable<T> Concat(IEnumerable<T> second) => v.AsEnumerable().Concat(second);
    }
}
