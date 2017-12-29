using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    /// <summary>
    /// Hold into a resource, allowing access only when locked
    /// </summary>
    internal class AsyncLockedResourceHolder<T>
    {
        /// <summary>
        /// Hold the value
        /// </summary>
        private T _value;

        /// <summary>
        /// Our async lock for access.
        /// </summary>
        private AsyncLock _lock = new AsyncLock();

        /// <summary>
        /// Initialize the locked resource
        /// </summary>
        /// <param name="initialValue">Value to initialize type T with.</param>
        public AsyncLockedResourceHolder(T initialValue)
        {
            _value = initialValue;
        }

        /// <summary>
        /// Apply a function to the value after the lock has been grabbed.
        /// </summary>
        /// <param name="accessFunc">Action to be applied; must return a task (e.g. be an async function)</param>
        /// <returns>A Task to await on</returns>
        public async Task ApplyAsync(Func<T, Task> accessFunc)
        {
            using (var holder = await _lock.LockAsync())
            {
                await accessFunc(_value);
            }
        }

        /// <summary>
        /// Apply a function to the value after the lock has been grabbed.
        /// </summary>
        /// <param name="accessFunc">Action to be applied; must return a task (e.g. be an async function)</param>
        /// <returns>A Task to await on</returns>
        public async Task ApplyAsync(Action<T> accessFunc)
        {
            using (var holder = await _lock.LockAsync())
            {
                accessFunc(_value);
            }
        }

        /// <summary>
        /// Apply a function to the value after the lock has been grabbed and return a value.
        /// </summary>
        /// <typeparam name="R">Return type of the funciton and the Apply</typeparam>
        /// <param name="accessFunc">Function to apply</param>
        /// <returns>A task holding the calculated value from accessFunc</returns>
        public async Task<R> ApplyAsync<R>(Func<T, Task<R>> accessFunc)
        {
            using (var holder = await _lock.LockAsync())
            {
                return await accessFunc(_value);
            }
        }

        /// <summary>
        /// Apply a function to the value after the lock has been grabbed and return a value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="accessFunc"></param>
        /// <returns></returns>
        public async Task<R> ApplyAsync<R>(Func<T, R> accessFunc)
        {
            using (var holder = await _lock.LockAsync())
            {
                return accessFunc(_value);
            }
        }

        /// <summary>
        /// Reset the held value to something new.
        /// </summary>
        /// <param name="replacementValue">A function that will generate a new value from the old value. The held value will be updated.</param>
        /// <returns>A task that can be awaited on.</returns>
        public async Task ResetValueAsync (Func<T, Task<T>> replacementValue)
        {
            using (var holder = await _lock.LockAsync())
            {
                _value = await replacementValue(_value);
            }
        }

        /// <summary>
        /// Reset the held value to something new.
        /// </summary>
        /// <param name="replacementValue">A function that will generate a new value from the old value. The held value will be updated.</param>
        /// <returns>A task that can be awaited on.</returns>
        public async Task ResetValueAsync(Func<T, T> replacementValue)
        {
            using (var holder = await _lock.LockAsync())
            {
                _value = replacementValue(_value);
            }
        }
    }
}
