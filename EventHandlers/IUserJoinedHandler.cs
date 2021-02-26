using Discord.WebSocket;
using System.Threading.Tasks;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// An object responsible for handling user joined events.
    /// </summary>
    public interface IUserJoinedHandler
    {
        /// <summary>
        /// Handle a user joined event.
        /// </summary>
        /// <param name="user">The user who joined.</param>
        /// <returns>When the task has finished.</returns>
        Task HandleUserJoin(SocketGuildUser user);
    }
}
