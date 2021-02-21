using System;
using System.Collections.Concurrent;

namespace Reiati.ChillBot.Tools
{
    /// <summary>
    /// A pool of objects.
    /// </summary>
    /// <typeparam name="T">Any type.</typeparam>
    /// <remarks>
    /// Lifted from
    /// https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool
    /// </remarks>
    public class ObjectPool<T>
    {
        /// <summary>
        /// The pool of objects.
        /// </summary>
        private readonly ConcurrentBag<T> _objects;

        /// <summary>
        /// The factory to generate Ts.
        /// </summary>
        private readonly Func<T> _objectGenerator;

        /// <summary>
        /// Constructs a <see cref="ObjetPool{T}"/>.
        /// </summary>
        /// <param name="tFactory">A function which generates Ts.</param>
        /// <param name="preallocate">An amount of Ts to preallocate.</param>
        public ObjectPool(Func<T> tFactory, uint preallocate = 0)
        {
            _objectGenerator = tFactory ?? throw new ArgumentNullException(nameof(tFactory));
            _objects = new ConcurrentBag<T>();
            
            for (uint i = 0; i < preallocate; i += 1)
            {
                _objects.Add(_objectGenerator());
            }
        }

        /// <summary>
        /// Removes an object from the object pool, and returns it.
        /// </summary>
        /// <returns>A T.</returns>
        public T Get() => _objects.TryTake(out T item) ? item : _objectGenerator();

        /// <summary>
        /// Returns an object to the object pool.
        /// </summary>
        /// <param name="item">A T.</param>
        public void Return(T item) => _objects.Add(item);
    }
}
