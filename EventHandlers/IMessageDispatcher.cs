using Discord.WebSocket;
using Reiati.ChillBot.Tools;
using System.Threading.Tasks;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// An object responsible for dispatching messages to a handler, if there is one which can handle the message.
    /// </summary>
    public interface IMessageDispatcher
    {
        /// <summary>
        /// The method to be subscribed to the client's MessageReceived event.
        /// </summary>
        /// <param name="message">The message received.</param>
        /// <returns>When the message has been handled.</returns>
        Task HandleMessageReceived(SocketMessage message);

        /// <summary>
        /// Set the dispatcher to require the provided bot ID be mentioned in order to distribute the message.
        /// </summary>
        /// <param name="botId">The ID of the bot that is required to be mentioned in the message.</param>
        void RequireMention(Snowflake botId);
    }
}
