Chill Bot
=========

A Discord bot which provides opt-in rooms.

A common problem with running a Discord server is that what might have started as a small group chat can turn into a small community of people, many of whom don't know each other. Some people thrive in an environment with large amount of strangers, but others do not, and will start to draw back. I found I was able to get the participation of some of those people back by limiting their audience size. By creating individual rooms that people can join, you can shrink a conversation's audience, without having to purposefully exclude anyone.

## How It Works

When you invite Chill Bot to your server and configure it appropriately, you can begin creating opt-in channels using [Discord slash commands](https://support.discord.com/hc/articles/1500000368501-Slash-Commands-FAQ) like so:

> /create donuts This channel is for people who love donuts.

Chill Bot will create a new private channel and role associated with that channel. By default, only the channel creator has that role, but other server members can join like so:

> /join donuts

When they do this, they will be given the role to view donuts, and just donuts. You can have as many opt-in channels as you want, and each channel will get its own role.

## How To Use
In Servers:
 - **/create \[channel name\] \[channel description\]** - Creates a new opt-in channel, and associated role.
 - **/join \[channel name\]** - Assigns that member the role to view the channel given.
 - **/leave \[channel name\]** - Removes the role to view the channel given from that user.
 - **/list** - Lists all of the opt-in channels.
 - **/rename \[current channel name\] \[new channel name\]** - Changes the name of an existing opt-in channel.
 - **/redescribe \[channel name\] \[channel description\]** - Changes the description of an existing opt-in channel.

## Planned Features
- [x] Opt-in Channels - Channels which server members can opt into, but are opted out by default.
- [x] Welcome Messages - Let new members know what rooms are available to them.
- [ ] Invite Only Rooms - Channels which all channel members can invite others into, but are not listed publicly.

## Contributing
Still in construction. Only accepting issues and bug fix PRs. See [Manufacturing.md](./Manufacturing.md) for instructions on how to get your machine set up to run this bot.
