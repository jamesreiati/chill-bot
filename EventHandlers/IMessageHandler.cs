using System;
using System.Threading.Tasks;
using Discord;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// An object which can handle messages.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Invoked when a message is received to determine whether or not this handler can handle the message.
        /// </summary>
        /// <param name="message">The message received.</param>
        /// <param name="recycleResult">A preallocated result that should be returned if passed in.</param>
        /// <returns>Whether or not this handler can handle the given message.</returns>
        Task<CanHandleResult> CanHandleMessage(IMessage message, CanHandleResult recycleResult = null);

        /// <summary>
        /// Invoked after this object has been determined to be handleable.
        /// </summary>
        /// <param name="message">The message received.</param>
        /// <param name="handleCache">
        /// The same object returned from the earlier call of <see cref="CanHandleMessage"/>.
        /// </param>
        /// <returns>The task to handle the message.</returns>
        Task HandleMessage(IMessage message, object handleCache);
    }
}
