using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// The common logic used for all message handlers which perform regex to determine if they can handle a message.
    /// </summary>
    public abstract class AbstractRegexHandler : IMessageHandler
    {
        /// <summary>
        /// The matcher for this handler.
        /// </summary>
        protected Regex Matcher { get; }

        /// <summary>
        /// Abstract constructor for <see cref="AbstractRegexHandler"/>. Must be invoked by implementers.
        /// </summary>
        /// <param name="matcher">The Regex for this handler.</param>
        protected AbstractRegexHandler(Regex matcher)
        {
            ValidateArg.IsNotNull(matcher, nameof(matcher));
            this.Matcher = matcher;
        }

        #pragma warning disable 1998 // This async method lacks 'await' operators and will run synchronously.

        /// <inheritdoc/>
        async Task<CanHandleResult> IMessageHandler.CanHandleMessage(
            SocketMessage message,
            CanHandleResult recycleResult)
        {
            var result = recycleResult ?? new CanHandleResult();
            try
            {
                var match = this.Matcher.Match(message.Content);
                if (match.Success)
                {
                    result.ToHandleable(match);
                    return result;
                }
                else
                {
                    result.ToUnhandleable();
                    return result;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                result.ToTimedOut(e.MatchTimeout);
                return result;
            }
        }

        #pragma warning restore 1998 // This async method lacks 'await' operators and will run synchronously.

        /// <inheritdoc/>
        Task IMessageHandler.HandleMessage(SocketMessage message, object handleCache)
        {
            return HandleMatchedMessage(message, (Match)handleCache);
        }

        /// <summary>
        /// Implementers should derive from this to handle a matched message.
        /// </summary>
        /// <param name="message">The message received.</param>
        /// <param name="handleCache">The match object returned from the regex match.</param>
        /// <returns>The handle task.</returns>
        abstract protected Task HandleMatchedMessage(SocketMessage message, Match handleCache);
    }
}
