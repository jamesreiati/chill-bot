using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.Behavior
{
    /// <summary>
    /// Responsible for managing behavior related to the opt-in channels.
    /// <para>
    /// Opt-in channels are channels which are by default hidden from a user, but are supposed to be advertised to all
    /// users. By default, a user will not have the role associated with an opt-in channel, but any user may recieve
    /// the permissions to join that channel.</para>
    /// </summary>
    /// <remarks>
    /// Opt-in channels must be made under an opt-in category. This category *must* give the bot permisison to view all
    /// channels, and *should* deny all users from viewing all channels.
    /// </remarks>
    public class OptinChannel
    {
        /// <summary>
        /// Creates an opt-in channel in the given guild.
        /// </summary>
        /// <param name="guildConnection">
        /// The connection to the guild this channel is being created in. May not be null.
        /// </param>
        /// <param name="guildData">Information about this guild. May not be null.</param>
        /// <param name="requestAuthor">The author of the channel create request. May not be null.</param>
        /// <param name="channelName">The requested name of the new channel. May not be null.</param>
        /// <param name="description">The requested description of the new channel.</param>
        /// <returns>The result of the request.</returns>
        public static async Task<CreateResult> Create(
            SocketGuild guildConnection,
            Guild guildData,
            SocketGuildUser requestAuthor,
            string channelName,
            string description)
        {
            ValidateArg.IsNotNullOrWhiteSpace(channelName, nameof(channelName));

            if (!guildData.OptinParentCategory.HasValue)
            {
                return CreateResult.NoOptinCategory;
            }
            var optinsCategory = guildData.OptinParentCategory.GetValueOrDefault();

            // TODO: requestAuthor.Roles gets cached. How do I refresh this value so that it's accurate?

            var hasPermission = PermissionsUtilities.HasPermission(
                userRoles: requestAuthor.Roles.Select(x => new Snowflake(x.Id)),
                allowedRoles: guildData.OptinCreatorsRoles);
            if (!hasPermission)
            {
                return CreateResult.NoPermissions;
            }

            var optinsCategoryConnection = guildConnection.GetCategoryChannel(optinsCategory.Value);
            var alreadyExists = optinsCategoryConnection.Channels
                .Select(x => x.Name)
                .Any(x => string.Compare(x, channelName, ignoreCase: false) == 0);
            if (alreadyExists)
            {
                return CreateResult.ChannelNameUsed;
            }

            var createdTextChannel = await guildConnection.CreateTextChannelAsync(channelName, settings =>
            {
                settings.CategoryId = optinsCategory.Value;
                settings.Topic = description ?? string.Empty;
            });

            var createdRole = await guildConnection.CreateRoleAsync(
                name: OptinChannel.GetRoleName(createdTextChannel.Id),
                permissions: null,
                color: null,
                isHoisted: false,
                isMentionable: false);

            var newPermissions = createdTextChannel.AddPermissionOverwriteAsync(
                role: createdRole,
                permissions: new OverwritePermissions(viewChannel: PermValue.Allow));

            await requestAuthor.AddRoleAsync(createdRole);

            return CreateResult.Success;
        }

        /// <summary>
        /// Joins an optin channel.
        /// </summary>
        /// <param name="guildConnection">
        /// The connection to the guild the user is trying to join a channel in. May not be null.
        /// </param>
        /// <param name="guildData">Information about this guild. May not be null.</param>
        /// <param name="requestAuthor">The author of the join channel request. May not be null.</param>
        /// <param name="channelName">The name of the channel to join.</param>
        /// <returns>The result of the request.</returns>
        public static async Task<JoinResult> Join(
            SocketGuild guildConnection,
            Guild guildData,
            SocketGuildUser requestAuthor,
            string channelName)
        {
            if (!guildData.OptinParentCategory.HasValue)
            {
                return JoinResult.NoOptinCategory;
            }
            var optinsCategory = guildData.OptinParentCategory.GetValueOrDefault();

            var optinsCategoryConnection = guildConnection.GetCategoryChannel(optinsCategory.Value);
            var requestedChannel = optinsCategoryConnection.Channels
                .FirstOrDefault(x => string.Compare(x.Name, channelName, ignoreCase: false) == 0);

            if (requestedChannel == null)
            {
                return JoinResult.NoSuchChannel;
            }
            
            var associatedRoleName = OptinChannel.GetRoleName(requestedChannel.Id);
            var role = guildConnection.Roles
                .FirstOrDefault(x => string.Compare(x.Name, associatedRoleName, ignoreCase: false) == 0);

            if (role == null)
            {
                return JoinResult.RoleMissing;
            }

            await requestAuthor.AddRoleAsync(role);
            return JoinResult.Success;
        }

        /// <summary>
        /// Lists all of the opt-in channels.
        /// </summary>
        /// <param name="guildConnection">A connection to the guild. May not be null.</param>
        /// <param name="guildData">Information about this guild. May not be null.</param>
        /// <param name="recycleResult">A preallocated result that should be returned if passed in.</param>
        /// <returns>All of the names and descriptions opt-in channels.</returns>
        public static ListResult List(
            SocketGuild guildConnection,
            Guild guildData,
            ListResult recycleResult = null)
        {
            var retVal = recycleResult ?? new ListResult();
            if (!guildData.OptinParentCategory.HasValue)
            {
                retVal.ToNoOptinCategory();
                return retVal;
            }
            var optinsCategory = guildData.OptinParentCategory.GetValueOrDefault();

            var optinsCategoryConnection = guildConnection.GetCategoryChannel(optinsCategory.Value);
            retVal.ToSuccess(
                optinsCategoryConnection.Channels
                // TODO: Still need to find the channel descriptions
                .Select(x => new Tuple<string, string>(x.Name, string.Empty)));
            return retVal;
        }

        /// <summary>
        /// Returns the role name associated with a given opt-in channel.
        /// </summary>
        /// <param name="optinChannel">An id representing an opt-in channel. Not verified against real channels.</param>
        /// <returns>The name of the role to be (if not already) associated with a given opt-in channel.</returns>
        public static string GetRoleName(Snowflake optinChannel)
        {
            return "chill-" + optinChannel.Value;
        }
        
        /// <summary>
        /// Result type of a <see cref="OptinChannel.Create(SocketGuild, Guild, SocketGuildUser, string, string)"/>
        /// call.
        /// </summary>
        public enum CreateResult
        {
            /// <summary>An optin channel was successfully created.</summary>
            Success,

            /// <summary></summary>
            NoOptinCategory,

            /// <summary></summary>
            NoPermissions,

            /// <summary></summary>,
            ChannelNameUsed
        }

        /// <summary>
        /// Result type of a <see cref="OptinChannel.Join(SocketGuild, Guild, SocketGuildUser, string)"/> call.
        /// </summary>
        public enum JoinResult
        {
            /// <summary>The user was given added to the opt-in channel.</summary>
            Success,

            /// <summary>The channel name given does not exist as an opt-in channel (or at all.)</summary>
            NoSuchChannel,

            /// <summary>The server has no Opt-in channel category.</summary>
            NoOptinCategory,

            /// <summary>The server has no role associated with the opt-in channel.</summary>
            RoleMissing,
        }

        /// <summary>
        /// The result of a <see cref="OptinChannel.List(SocketGuild, Guild, ListResult)"/> call.
        /// </summary>
        public sealed class ListResult
        {
            /// <summary>
            /// Underlying list.
            /// </summary>
            private readonly List<NameDescription> namesDescriptions = new List<NameDescription>(5);

            /// <summary>
            /// The type of this result.
            /// </summary>
            public ResultType Result { get; private set; }

            /// <summary>
            /// The names and descriptions of all the opt-in channels that can be joined.
            /// </summary>
            public IReadOnlyList<NameDescription> NamesDescriptions => this.namesDescriptions;

            /// <summary>
            /// Set this result to the <see cref="ResultType.Success"/> type.
            /// </summary>
            /// <param name="namesDescriptions">Some names and descriptions of channels.</param>
            public void ToSuccess(IEnumerable<Tuple<string, string>> namesDescriptions)
            {
                this.Result = ResultType.Success;
                this.namesDescriptions.Clear();
                this.namesDescriptions.AddRange(
                    namesDescriptions
                    .Select(x => new NameDescription(
                        name: x.Item1 ?? string.Empty,
                        description: x.Item2 ?? string.Empty)));
            }

            /// <summary>
            /// Set this result to the <see cref="ResultType.NoOptinCategory"/> type.
            /// </summary>
            public void ToNoOptinCategory()
            {
                this.Result = ResultType.NoOptinCategory;
            }

            /// <summary>
            /// Drops all references to objects.
            /// </summary>
            /// <remarks>Useful call before returning to a pool.</remarks>
            public void ClearReferences()
            {
                this.namesDescriptions.Clear();
            }

            /// <summary>
            /// The result of a <see cref="OptinChannel.List(SocketGuild, Guild, ListResult)"/> call.
            /// </summary>
            public enum ResultType
            {
                /// <summary>The channels are able to be listed.</summary>
                Success,

                /// <summary>The server has no Opt-in channel category.</summary>
                NoOptinCategory,
            }

            /// <summary>
            /// A name and description of a channel.
            /// </summary>
            public readonly struct NameDescription
            {
                /// <summary>
                /// Name of the channel.
                /// </summary>
                public readonly string name;

                /// <summary>
                /// Description for the channel.
                /// </summary>
                public readonly string description;

                /// <summary>
                /// Constructs a <see cref="NameDescription"/>.
                /// </summary>
                /// <param name="name">The name of the channel.</param>
                /// <param name="description">The description for the channel.</param>
                public NameDescription(string name, string description)
                {
                    this.name = name;
                    this.description = description;
                }
            }
        }
    }
}
