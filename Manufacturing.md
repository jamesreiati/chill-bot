> This document contains instructions on how to get your machine to build and start the service. All of the below commands are run from the git repository's base directory unless stated otherwise.

1. Download and install the .NET Core 3.1 SDK and runtime from [Microsoft](https://dotnet.microsoft.com/download).

2. Make a copy of `log4net.example.xml` and rename it to `log4net.config.xml`. Make any changes to the way logs are displayed here.
```
> cp .\log4net.example.xml .\log4net.config.xml
```

3. Make a copy of `discordtoken.example.txt` and rename it to `discordtoken.txt`. Replace the contents of this file with your bot token.
```
> cp .\discordtoken.example.txt .\discordtoken.txt
```

4. Run the project.
```
> dotnet run
```
