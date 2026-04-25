using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Farsight.Common.Extensions;

/// <summary>
/// Common extensions used in Farsight applications.
/// </summary>
public static partial class Extensions
{
    extension<T>(ReadOnlyMemory<T> memory)
    {
        /// <summary>
        /// Creates an <see cref="IEnumerable{T}"/> view of the given read-only memory buffer.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> view of the given read-only memory buffer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> ToEnumerable()
            => MemoryMarshal.ToEnumerable(memory);

        /// <summary>
        /// Applies an accumulator function over the elements of the memory.
        /// </summary>
        /// <param name="func">The accumulator function to apply.</param>
        /// <returns>The final accumulated value.</returns>
        public T Aggregate(Func<T, T, T> func)
            => memory.ToEnumerable().Aggregate(func);

        /// <summary>
        /// Applies an accumulator function over the elements of the memory using the specified seed value.
        /// </summary>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">The accumulator function to apply to each element.</param>
        /// <returns>The final accumulator value.</returns>
        public TAccumulate Aggregate<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> func)
            => memory.ToEnumerable().Aggregate(seed, func);

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
            => memory.ToEnumerable().Aggregate(seed, func, resultSelector);

        /// <summary>
        /// Determines whether all elements in the memory satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns><see langword="true"/> if every element matches the predicate; otherwise, <see langword="false"/>.</returns>
        public bool All(Func<T, bool> predicate)
            => memory.ToEnumerable().All(predicate);

        /// <summary>
        /// Determines whether the memory contains any elements.
        /// </summary>
        /// <returns><see langword="true"/> if the memory contains at least one element; otherwise, <see langword="false"/>.</returns>
        public bool Any()
            => memory.Length > 0;

        /// <summary>
        /// Determines whether any element in the memory satisfies the specified predicate.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns><see langword="true"/> if any element matches the predicate; otherwise, <see langword="false"/>.</returns>
        public bool Any(Func<T, bool> predicate)
            => memory.ToEnumerable().Any(predicate);

        /// <summary>
        /// Concatenates the memory with another sequence.
        /// </summary>
        /// <param name="second">The sequence to concatenate to the memory.</param>
        /// <returns>A sequence that contains the elements of the memory followed by the elements of <paramref name="second"/>.</returns>
        public IEnumerable<T> Concat(IEnumerable<T> second)
            => memory.ToEnumerable().Concat(second);

        /// <summary>
        /// Concatenates the memory with another sequence.
        /// </summary>
        /// <param name="second">The sequence to concatenate to the memory.</param>
        /// <returns>A sequence that contains the elements of the memory followed by the elements of <paramref name="second"/>.</returns>
        public IEnumerable<T> Concat(ReadOnlyMemory<T> second)
            => memory.ToEnumerable().Concat(second.ToEnumerable());

        /// <summary>
        /// Returns a number that represents how many elements in the memory satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns>A number that represents how many elements in the memory satisfy the predicate.</returns>
        public int Count(Func<T, bool> predicate)
            => memory.ToEnumerable().Count(predicate);

        /// <summary>
        /// Returns distinct elements from the memory.
        /// </summary>
        /// <returns>A sequence that contains distinct elements from the memory.</returns>
        public IEnumerable<T> Distinct()
            => memory.ToEnumerable().Distinct();

        /// <summary>
        /// Returns distinct elements from the memory by using a specified <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>A sequence that contains distinct elements from the memory.</returns>
        public IEnumerable<T> Distinct(IEqualityComparer<T>? comparer)
            => memory.ToEnumerable().Distinct(comparer);

        /// <summary>
        /// Returns the first element in the memory that satisfies the specified predicate.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns>The first element that satisfies the predicate.</returns>
        public T First(Func<T, bool> predicate)
            => memory.ToEnumerable().First(predicate);

        /// <summary>
        /// Returns the first element of the memory that satisfies the specified predicate, or a default value if no such element is found.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns><see langword="default"/> if no element satisfies the predicate; otherwise, the first element that satisfies the predicate.</returns>
        public T? FirstOrDefault(Func<T, bool> predicate)
            => memory.ToEnumerable().FirstOrDefault(predicate);

        /// <summary>
        /// Returns an enumerator for this <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpan<T>.Enumerator GetEnumerator()
            => memory.Span.GetEnumerator();

        /// <summary>
        /// Groups the elements of the memory according to a specified key selector.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract the key for each element.</param>
        /// <returns>A sequence of groups whose keys are produced by the key selector.</returns>
        public IEnumerable<IGrouping<TKey, T>> GroupBy<TKey>(Func<T, TKey> keySelector)
            => memory.ToEnumerable().GroupBy(keySelector);

        /// <summary>
        /// Groups the elements of the memory according to a key selector and projects each element into a new form.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each <see cref="IGrouping{TKey, TElement}"/>.</typeparam>
        /// <param name="keySelector">The function used to extract the key for each element.</param>
        /// <param name="elementSelector">The function used to map each source element to an element in an <see cref="IGrouping{TKey, TElement}"/>.</param>
        /// <returns>A sequence of groups whose keys are produced by the key selector and whose elements are produced by the element selector.</returns>
        public IEnumerable<IGrouping<TKey, TElement>> GroupBy<TKey, TElement>(Func<T, TKey> keySelector, Func<T, TElement> elementSelector)
            => memory.ToEnumerable().GroupBy(keySelector, elementSelector);

        /// <summary>
        /// Returns the last element in the memory that satisfies the specified predicate.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns>The last element that satisfies the predicate.</returns>
        public T Last(Func<T, bool> predicate)
            => memory.ToEnumerable().Last(predicate);

        /// <summary>
        /// Returns the last element of the memory that satisfies the specified predicate, or a default value if no such element is found.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns><see langword="default"/> if no element satisfies the predicate; otherwise, the last element that satisfies the predicate.</returns>
        public T? LastOrDefault(Func<T, bool> predicate)
            => memory.ToEnumerable().LastOrDefault(predicate);

        /// <summary>
        /// Returns a <see cref="UInt64"/> that represents how many elements in the memory satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns>A number that represents how many elements in the memory satisfy the predicate.</returns>
        public long LongCount(Func<T, bool> predicate)
            => memory.ToEnumerable().LongCount(predicate);

        /// <summary>
        /// Returns the maximum value in the memory.
        /// </summary>
        /// <returns>The maximum value in the memory.</returns>
        public T? Max()
            => memory.ToEnumerable().Max();

        /// <summary>
        /// Invokes a transform function on each element of the memory and returns the maximum value.
        /// </summary>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
        /// <param name="selector">The transform function to apply to each element.</param>
        /// <returns>The maximum value in the projected sequence.</returns>
        public TResult? Max<TResult>(Func<T, TResult> selector)
            => memory.ToEnumerable().Max(selector);

        /// <summary>
        /// Returns the minimum value in the memory.
        /// </summary>
        /// <returns>The minimum value in the memory.</returns>
        public T? Min()
            => memory.ToEnumerable().Min();

        /// <summary>
        /// Invokes a transform function on each element of the memory and returns the minimum value.
        /// </summary>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
        /// <param name="selector">The transform function to apply to each element.</param>
        /// <returns>The minimum value in the projected sequence.</returns>
        public TResult? Min<TResult>(Func<T, TResult> selector)
            => memory.ToEnumerable().Min(selector);

        /// <summary>
        /// Sorts the elements of the memory in ascending order according to a key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract a sort key from each element.</param>
        /// <returns>An ordered sequence of the memory elements.</returns>
        public IOrderedEnumerable<T> OrderBy<TKey>(Func<T, TKey> keySelector)
            => memory.ToEnumerable().OrderBy(keySelector);

        /// <summary>
        /// Sorts the elements of the memory in ascending order according to a key and comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An ordered sequence of the memory elements.</returns>
        public IOrderedEnumerable<T> OrderBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer)
            => memory.ToEnumerable().OrderBy(keySelector, comparer);

        /// <summary>
        /// Sorts the elements of the memory in descending order according to a key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract a sort key from each element.</param>
        /// <returns>An ordered sequence of the memory elements.</returns>
        public IOrderedEnumerable<T> OrderByDescending<TKey>(Func<T, TKey> keySelector)
            => memory.ToEnumerable().OrderByDescending(keySelector);

        /// <summary>
        /// Sorts the elements of the memory in descending order according to a key and comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An ordered sequence of the memory elements.</returns>
        public IOrderedEnumerable<T> OrderByDescending<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer)
            => memory.ToEnumerable().OrderByDescending(keySelector, comparer);

        /// <summary>
        /// Projects each element of the memory into a new form.
        /// </summary>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
        /// <param name="selector">The transform function to apply to each element.</param>
        /// <returns>A sequence whose elements are the result of invoking the transform function on each element.</returns>
        public IEnumerable<TResult> Select<TResult>(Func<T, TResult> selector)
            => memory.ToEnumerable().Select(selector);

        /// <summary>
        /// Projects each element of the memory into a new form by incorporating the element index.
        /// </summary>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
        /// <param name="selector">The transform function to apply to each element and its index.</param>
        /// <returns>A sequence whose elements are the result of invoking the transform function on each element and its index.</returns>
        public IEnumerable<TResult> Select<TResult>(Func<T, int, TResult> selector)
            => memory.ToEnumerable().Select(selector);

        /// <summary>
        /// Projects each element of the memory into a sequence and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by <paramref name="selector"/>.</typeparam>
        /// <param name="selector">The transform function that returns a sequence for each element.</param>
        /// <returns>A sequence whose elements are the concatenated results of invoking the transform function on each element.</returns>
        public IEnumerable<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector)
            => memory.ToEnumerable().SelectMany(selector);

        /// <summary>
        /// Projects each element of the memory into a sequence and flattens the resulting sequences into one sequence, using the element index.
        /// </summary>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by <paramref name="selector"/>.</typeparam>
        /// <param name="selector">The transform function that returns a sequence for each element and its index.</param>
        /// <returns>A sequence whose elements are the concatenated results of invoking the transform function on each element and its index.</returns>
        public IEnumerable<TResult> SelectMany<TResult>(Func<T, int, IEnumerable<TResult>> selector)
            => memory.ToEnumerable().SelectMany(selector);

        /// <summary>
        /// Projects each element of the memory into an intermediate sequence and invokes a result selector on each item of each intermediate sequence.
        /// </summary>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="collectionSelector">The transform function that returns a sequence for each element.</param>
        /// <param name="resultSelector">The transform function that projects each pair into a result element.</param>
        /// <returns>A sequence whose elements are the result of invoking the one-to-many transform function on each element and then mapping each of those sequence elements and their corresponding source element to a result element.</returns>
        public IEnumerable<TResult> SelectMany<TCollection, TResult>(Func<T, IEnumerable<TCollection>> collectionSelector, Func<T, TCollection, TResult> resultSelector)
            => memory.ToEnumerable().SelectMany(collectionSelector, resultSelector);

        /// <summary>
        /// Returns the only element of the memory that satisfies the specified predicate, and throws an exception if more than one such element exists.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns>The single element that satisfies the predicate.</returns>
        public T Single(Func<T, bool> predicate)
            => memory.ToEnumerable().Single(predicate);

        /// <summary>
        /// Returns the only element of the memory that satisfies the specified predicate, or a default value if no such element exists; throws if more than one element satisfies the predicate.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns>The single element that satisfies the predicate, or <see langword="default"/> if no such element is found.</returns>
        public T? SingleOrDefault(Func<T, bool> predicate)
            => memory.ToEnumerable().SingleOrDefault(predicate);

        /// <summary>
        /// Bypasses elements in the memory as long as a specified predicate is true and then returns the remaining elements.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns>A sequence that contains the elements from the memory starting at the first element that does not pass the test.</returns>
        public IEnumerable<T> SkipWhile(Func<T, bool> predicate)
            => memory.ToEnumerable().SkipWhile(predicate);

        /// <summary>
        /// Returns elements from the memory as long as a specified predicate is true, and then skips the remaining elements.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns>A sequence that contains the elements from the memory that occur before the element at which the test no longer passes.</returns>
        public IEnumerable<T> TakeWhile(Func<T, bool> predicate)
            => memory.ToEnumerable().TakeWhile(predicate);

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from the elements in the memory according to a specified key selector.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract a key from each element.</param>
        /// <returns>A dictionary that contains keys and values derived from the memory.</returns>
        public Dictionary<TKey, T> ToDictionary<TKey>(Func<T, TKey> keySelector) where TKey : notnull
            => memory.ToEnumerable().ToDictionary(keySelector);

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from the elements in the memory according to a specified key selector and key comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract a key from each element.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys.</param>
        /// <returns>A dictionary that contains keys and values derived from the memory.</returns>
        public Dictionary<TKey, T> ToDictionary<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
            => memory.ToEnumerable().ToDictionary(keySelector, comparer);

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from the elements in the memory according to specified key and element selector functions.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the values returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract a key from each element.</param>
        /// <param name="elementSelector">The transform function used to produce a result element value from each element.</param>
        /// <returns>A dictionary that contains values of type <typeparamref name="TElement"/> selected from the memory.</returns>
        public Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(Func<T, TKey> keySelector, Func<T, TElement> elementSelector) where TKey : notnull
            => memory.ToEnumerable().ToDictionary(keySelector, elementSelector);

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from the elements in the memory according to specified key selector, element selector, and key comparer functions.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the values returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract a key from each element.</param>
        /// <param name="elementSelector">The transform function used to produce a result element value from each element.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys.</param>
        /// <returns>A dictionary that contains values of type <typeparamref name="TElement"/> selected from the memory.</returns>
        public Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(Func<T, TKey> keySelector, Func<T, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
            => memory.ToEnumerable().ToDictionary(keySelector, elementSelector, comparer);

        /// <summary>
        /// Creates a <see cref="HashSet{T}"/> from the elements in the memory.
        /// </summary>
        /// <returns>A hash set that contains the elements of the memory.</returns>
        public HashSet<T> ToHashSet()
            => [.. memory.ToEnumerable()];

        /// <summary>
        /// Creates a <see cref="HashSet{T}"/> from the elements in the memory by using a specified <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing values.</param>
        /// <returns>A hash set that contains the elements of the memory.</returns>
        public HashSet<T> ToHashSet(IEqualityComparer<T>? comparer)
            => memory.ToEnumerable().ToHashSet(comparer);

        /// <summary>
        /// Creates a <see cref="List{T}"/> from the elements in the memory.
        /// </summary>
        /// <returns>A list that contains the elements of the memory.</returns>
        public List<T> ToList()
            => [.. memory.ToEnumerable()];

        /// <summary>
        /// Filters the elements of the memory based on the specified predicate.
        /// </summary>
        /// <param name="predicate">The function used to test each element.</param>
        /// <returns>A sequence that contains the elements that satisfy the predicate.</returns>
        public IEnumerable<T> Where(Func<T, bool> predicate)
            => memory.ToEnumerable().Where(predicate);

        /// <summary>
        /// Filters the elements of the memory based on the specified predicate that incorporates the element index.
        /// </summary>
        /// <param name="predicate">The function used to test each element and its index.</param>
        /// <returns>A sequence that contains elements from the memory that satisfy the predicate.</returns>
        public IEnumerable<T> Where(Func<T, int, bool> predicate)
            => memory.ToEnumerable().Where(predicate);

        /// <summary>
        /// Merges the memory with another sequence, producing tuples of paired elements.
        /// </summary>
        /// <typeparam name="TSecond">The type of the elements of the second sequence.</typeparam>
        /// <param name="second">The second sequence to merge.</param>
        /// <returns>A sequence of tuples containing elements from the memory and <paramref name="second"/>.</returns>
        public IEnumerable<(T First, TSecond Second)> Zip<TSecond>(IEnumerable<TSecond> second)
            => memory.ToEnumerable().Zip(second);

        /// <summary>
        /// Merges the memory with another sequence, producing tuples of paired elements.
        /// </summary>
        /// <typeparam name="TSecond">The type of the elements of the second sequence.</typeparam>
        /// <param name="second">The second sequence to merge.</param>
        /// <returns>A sequence of tuples containing elements from the memory and <paramref name="second"/>.</returns>
        public IEnumerable<(T First, TSecond Second)> Zip<TSecond>(ReadOnlyMemory<TSecond> second)
            => memory.ToEnumerable().Zip(second.ToEnumerable());

        /// <summary>
        /// Merges the memory with another sequence using a specified result selector.
        /// </summary>
        /// <typeparam name="TSecond">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the result sequence.</typeparam>
        /// <param name="second">The second sequence to merge.</param>
        /// <param name="resultSelector">The function that specifies how to merge the elements from the two sequences.</param>
        /// <returns>A sequence that contains merged elements of both sequences.</returns>
        public IEnumerable<TResult> Zip<TSecond, TResult>(IEnumerable<TSecond> second, Func<T, TSecond, TResult> resultSelector)
            => memory.ToEnumerable().Zip(second, resultSelector);

        /// <summary>
        /// Merges the memory with another sequence using a specified result selector.
        /// </summary>
        /// <typeparam name="TSecond">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the result sequence.</typeparam>
        /// <param name="second">The second sequence to merge.</param>
        /// <param name="resultSelector">The function that specifies how to merge the elements from the two sequences.</param>
        /// <returns>A sequence that contains merged elements of both sequences.</returns>
        public IEnumerable<TResult> Zip<TSecond, TResult>(ReadOnlyMemory<TSecond> second, Func<T, TSecond, TResult> resultSelector)
            => memory.ToEnumerable().Zip(second.ToEnumerable(), resultSelector);
    }
}
