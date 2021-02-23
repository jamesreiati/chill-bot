using System;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// The response of a MessageHandler's CanHandle method.
    /// </summary>
    /// <remarks>Designed to be poolable. Mimics the structure of a discriminated union.</remarks>
    public sealed class CanHandleResult
    {
        /// <summary>
        /// The type of this result.
        /// </summary>
        public ResultStatus Status { get; private set; }

        /// <summary>
        /// [Handleable] An arbitrary object to be sent back to the handler when handling resumes.
        /// </summary>
        public object HandleCache { get; private set; }

        /// <summary>
        /// [TimedOut] The period of time the timeout was set for.
        /// </summary>
        public TimeSpan TimeOutPeriod { get; private set; }

        /// <summary>
        /// Set this result to the [Handleable] type.
        /// </summary>
        /// <param name="handleCache">An arbitrary object to be sent back to the handler when handling resumes.</param>
        public void ToHandleable(object handleCache)
        {
            this.Status = ResultStatus.Handleable;
            this.HandleCache = handleCache;
        }

        /// <summary>
        /// Set this result to the [Unhandleable] type.
        /// </summary>
        public void ToUnhandleable()
        {
            this.Status = ResultStatus.Unhandleable;
        }

        /// <summary>
        /// Set this result to the [TimedOut] type.
        /// </summary>
        /// <param name="timeOutPeriod">The period of time the timeout was set for.</param>
        public void ToTimedOut(TimeSpan timeOutPeriod)
        {
            this.Status = ResultStatus.TimedOut;
            this.TimeOutPeriod = timeOutPeriod;
        }

        /// <summary>
        /// Drops all references to objects.
        /// </summary>
        /// <remarks>Useful call before returning to a pool.</remarks>
        public void ClearReferences()
        {
            this.HandleCache = null;
        }

        /// <summary>
        /// Response of a Message Handler.
        /// </summary>
        public enum ResultStatus
        {
            /// <summary>The message was able to be handled.</summary>
            Handleable,

            /// <summary>The message was not abled to be handled.</summary>
            Unhandleable,

            /// <summary>
            /// Whether or not this message could be handled was not able to be determined as the handler timed out.
            /// </summary>
            TimedOut,
        }
    }
}
