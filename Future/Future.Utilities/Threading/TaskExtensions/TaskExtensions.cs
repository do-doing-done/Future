using System;
using System.Threading;
using System.Threading.Tasks;

namespace Future.Utilities.Threading
{
    /// <summary>
    /// <see cref="Task" /> and <see cref="Task{TResult}" /> Extensions
    /// </summary>
    public static class TaskExtensions
    {
        #region [ Wait ]
        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="timeout"></param>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="TimeoutException"></exception>
        public static async Task WaitAsync(this Task task, TimeSpan timeout)
        {
            /* Check parameter. */
            if (null == task)    throw new ArgumentNullException($"This parameter {nameof(task)} is null.");
            if (null == timeout) throw new ArgumentNullException($"This parameter {nameof(timeout)} is null.");

            /* Check task state. */
            if (task.IsCompleted)                              return;
            if (Timeout.Infinite == timeout.TotalMilliseconds) return;

            /* Do. */
            using (var toke_source = new CancellationTokenSource())
            {
                if (task == await Task.WhenAny(task, Task.Delay(timeout, toke_source.Token)))
                {
                    toke_source.Cancel();
                    await task;
                }
                else
                {
                    throw new TimeoutException("The opertion has timed out.");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="task"></param>
        /// <param name="timeout"></param>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="TimeoutException"></exception>
        public static async Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            /* Check parameter. */
            if (null == task)     throw new ArgumentNullException($"This parameter {nameof(task)} is null.");
            if (null == timeout)  throw new ArgumentNullException($"This parameter {nameof(timeout)} is null.");

            /* Check task state. */
            if (task.IsCompleted)                              return await task;
            if (Timeout.Infinite == timeout.TotalMilliseconds) return await task;

            /* Do. */
            using (var toke_source = new CancellationTokenSource())
            {
                if (task == await Task.WhenAny(task, Task.Delay(timeout, toke_source.Token)))
                {
                    toke_source.Cancel();
                    return await task;
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }
        #endregion

        #region [ Forget ]
        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        public static void Forget(this Task task)
        {
            // Do nothing.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        public static void Forget<TResult>(this Task<TResult> task)
        {
            // Do nothing.
        }
        #endregion
    }
}
