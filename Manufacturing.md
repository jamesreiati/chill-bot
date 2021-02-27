> This document contains instructions on how to get your machine to build and start the service. All of the below commands are run from the git repository's base directory unless stated otherwise.

# Set Up Your Dev Bot

1. Log in to Discord and go to the [Developer Portal](https://discord.com/developers/applications).

2. Create a new application, and click into it to start configuring it.

3. Go to the bot settings, and make it a bot. The default settings are fine except for the following changes:
 - Under Privileged Gateway Intents, enable Server Members Intent. (Needed for welcome message.)

4. Copy your API token to a safe place to use later.

5. Use Discord's [Permissions Calculator](https://discordapi.com/permissions.html#268577872) to create a URL you can use to invite your bot to your own development server. The link provided should already set the permissions correctly for what the bot needs to operate, you just need to enter your Client ID (found in your bot settings.)

# Optional: Set Up Your Discord Client

1. Under User Settings -> Appearance -> Advanced, enable "Developer Mode". This will enable new options on the context menus (right click) which help you copy IDs of things.

# Set Up Your "Database"
> The backend of the bot currently uses JSON files, not a real database.

1. Create a new directory called `file-data`.
```
> mkdir file-data
```

2. Create a sub directory in there called `guilds`.
```
> cd file-data
> mkdir guilds
```

3. Create and open a new JSON file, the name of which should be the ID of your discord server. (The example below uses notepad as the editor, but any editor will do.)
```
> cd guilds
> notepad 123456789.json
```

4. Paste the following contents into it and save.
```
{}
```

> Note: Due to the changing nature of this application, you will have to dig through the code to fill the contents with valid data. Consider starting at `./Data/FileBasedGuildRepository.cs`.

# Set Up Your Machine

1. Download and install the .NET Core 3.1 SDK and runtime from [Microsoft](https://dotnet.microsoft.com/download).

2. Make a copy of `log4net.example.xml` and rename it to `log4net.config.xml`. Make any changes to the way logs are displayed here.
```
> cp .\log4net.example.xml .\log4net.config.xml
```

3. Make a copy of `config.json` and rename it to `config.Local.json`. Replace the value of the `DiscordToken` property in this file with your API token.
```
> cp .\config.json .\config.Local.json
```

4. (Optional) If you would like to customize the way logs are displayed, edit the `Logging` section of `config.Local.json` as described [here](https://docs.microsoft.com/dotnet/core/extensions/logging#configure-logging).

5. (Optional) If you would like to send logs to [Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview), edit the `config.Local.json` file and replace the value of the `ApplicationInsights:InstrumentationKey` property with your instrumentation key. You may also control the level of logs sent to Application Insights by adding filter rules to the `Logging` section of the file as described [here](https://docs.microsoft.com/azure/azure-monitor/app/ilogger#create-filter-rules-in-configuration-with-appsettingsjson).

6. Run the project.
```
> dotnet run
```
