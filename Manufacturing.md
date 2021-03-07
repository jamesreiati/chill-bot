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

> Note: If you would prefer to use Azure Blob Storage to store these files, upload the JSON file you created to an Azure Blob Storage Container and configure your bot instance to use Azure Blob Storage in the next section.

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

6. (Optional) If you would like to use [Azure Blob Storage](https://docs.microsoft.com/azure/storage/blobs/storage-blobs-introduction) as your guild repository, modify the `GuildRepository` section of the `config.Local.json` file to match the example below:
```json
"GuildRepository": {
  "Type": "AzureBlob",
  "AzureBlob": {
    "ConnectionString": "YOUR_CONNECTION_STRING_HERE",
    "Container": "guilds"
  }
}
```

7. Run the project.
```
> dotnet run
```

# Optional: Deploying to Azure from GitHub Actions
> This is just one of many ways to deploy the Chill Bot service to Azure. The use of the VMSS platform and the VMSS configuration chosen were primarily motivated by keeping the cost of running the bot minimial.

## Prerequisites

- An Azure subscription
  - If you don't have one, [sign up for free trial](https://azure.microsoft.com/free/).
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) for ease of following the instructions (but the Azure web portal would work too)
  - You may also want the application-insights extension for Azure CLI: `az extension add --name application-insights`
- A fork of this repository so you can populate your own secrets for your deployment environment

## Log in to Azure CLI and set the active Azure subscription
Log in to Azure CLI using the command below or [one of the many other log in options](https://docs.microsoft.com/cli/azure/reference-index#az_login).

```
az login
```

If you have a single Azure subscription associated with your account, you can likely skip this next command. That said, it's always a good idea to ensure you're running the remainder of the commands below against the expected subscription.

> If you don't know your subscription name or ID, you can list your subscriptions using `az account list --output table`.

```
az account set --subscription YOUR_SUBSCRIPTION_NAME_OR_ID_HERE
```

## Create supporting resources

Create the following Azure resource in your subscription and configure them to your liking:

| Resource        | Example Azure CLI command | Purpose |
| --------------- | ------------------------- | ------- |
| Resource Group | `az group create --name ChillBot --location WestUS2` | Group your resource together and limit the scope of access to your Subscription from GitHub actions |
| Storage Account | `az storage account create --resource-group ChillBot --name chillbotstorage --location WestUS2 --sku Standard_GRS` | Store Discord guild metadata used by Chill Bot |
| Application Insights (Optional) | `az monitor app-insights component create --resource-group ChillBot --app ChillBot --location WestUS2` | Collect and easily view logs from Chill Bot |
| Key Vault | `az keyvault create --resource-group ChillBot --name ChillBotKeyVault --location WestUS2` | Store secrets used by the bot to access Azure resources |
| Managed Identity | `az identity create --resource-group ChillBot --name ChillBotIdentity` | Use to grant the VMSS access to the Key Vault |
| Network Security Group (NSG) | `az network nsg create --resource-group ChillBot --name ChillBotNsg` | Use to restrict access to your VMSS for added security |
| Public IP (Optional) | `az network public-ip create --resource-group ChillBot --name ChillBotIP` | Use make your VMSS routable from the public internet (this is only needed if you plan to SSH into the VMSS) |

## Grant the Managed Identity access to read Key Vault secrets

The Managed Identity created above will need to be given permissions to read secrets from Key Vault. This can be achieved using an Azure CLI command like the one below:

```
az keyvault set-policy --resource-group ChillBot --name ChillBotKeyVault --secret-permissions get --object-id $(az identity show --resource-group ChillBot --name ChillBotIdentity --query principalId --out tsv)
```


## Populate secrets in Key Vault

Populate the following secrets in your Key Vault:

| Secret          | Example Azure CLI command |
| --------------- | ------------------------- |
| DiscordToken    | `az keyvault secret set --vault-name ChillBotKeyVault --name DiscordToken --value "abcdefghijklmnopqrstuvwxyz"` |
| GuildRepositoryConnectionString | `az keyvault secret set --vault-name ChillBotKeyVault --name GuildRepositoryConnectionString --value $(az storage account show-connection-string --resource-group ChillBot --name chillbotstorage --output tsv)` |
| ApplicationInsightsInstrumentationKey (Optional) | `az keyvault secret set --vault-name ChillBotKeyVault --name GuildRepositoryConnectionString --value $(az monitor app-insights component show --resource-group ChillBot --app ChillBot --query instrumentationKey --output tsv)` |

## Populate secrets in GitHub

Store secrets and other parameters for the [Deploy to Azure](./.github/workflows/azure-deploy.yml) action using [GitHub's encrypted secrets](https://docs.github.com/actions/reference/encrypted-secrets).

1. Create an environment in your respository named "Azure" by following [these instructions](https://docs.github.com/actions/reference/environments#creating-an-environment).
2. Populate the following secrets in your "Azure" environment by following [these instructions](https://docs.github.com/actions/reference/encrypted-secrets#creating-encrypted-secrets-for-a-repository).

    | Secret          | Where to obtain value     |
    | --------------- | ------------------------- |
    | `APPLICATION_INSIGHTS_KEY_SECRET_NAME` | The name of your Application Insight secret in Key Vault. If you followed the secret population above, this will be `ApplicationInsightsInstrumentationKey` |
    | `AZURE_CREDENTIALS` | Follow [these instructions](https://github.com/marketplace/actions/azure-login#configure-deployment-credentials). For the `--scope` parameter, provide the ID of the resource group you created above (or a separate resource group if prefer to keep the compute separate from the other resources) |
    | `BLOB_CONNECTION_SECRET_NAME` | The name of your guild repository secret in Key Vault. If you followed the secret population above, this will be `GuildRepositoryConnectionString` |
    | `DISCORD_TOKEN_SECRET_NAME` | The name of your Discord token secret in Key Vault. If you followed the secret population above, this will be `DiscordToken` |
    | `KEY_VAULT_NAME` | The name of the Key Vault resource you created above (e.g., `ChillBotKeyVault`). | `MANAGED_IDENTITY_ID` | The resource ID of the Managed Identity you created above. You can use Azure CLI to obtain this: `az identity show --resource-group ChillBot --name ChillBotIdentity --query id --out tsv` |
    | `NSG_ID` | The resource ID of the Network Security Group you created above (or omit this secret if you want to provision the VMSS without a NSG). You can use Azure CLI to obtain this: `az network nsg show --resource-group Chill-Bot --name ChillBotNsg --query id --out tsv` |
    | `PUBLIC_IP_ID` (Optional) | The resource ID of the Public IP you created above (or omit this secret if you want to provision the VMSS without a public IP). You can use Azure CLI to obtain this: `az network public-ip show --resource-group Chill-Bot --name ChillBotIP --query id --out tsv` |
    | `SSH_PUBLIC_KEY` | See [Provide SSH public key when deploying a VM](https://docs.microsoft.com/azure/virtual-machines/linux/create-ssh-keys-detailed#provide-ssh-public-key-when-deploying-a-vm). If you already have an SSH key, you can likely find it at `%USERPROFILE%\.ssh\id_rsa.pub` on Windows or at `~/.ssh/id_rsa.pub` on Linux. If you do not have SSH keys or wish to generate new SSH keys, follow [these instructions](https://docs.microsoft.com/azure/virtual-machines/linux/create-ssh-keys-detailed#generate-keys-with-ssh-keygen). |
    | `VMSS_LOCATION` | The name of the Azure region where you wish to deploy the compute for the VMSS (e.g., `WestUS2`). You can use `az account list-locations --output table` to see all of the options. |
    | `VMSS_NAME` | The name of the VMSS to deploy (e.g., `ChillBotDeployment`). |
    | `VMSS_RESOURCE_GROUP` | The name of the resource group you provided as the `--scope` when generating your `AZURE_CREDENTIALS` secret. If you used the resource group created above, this would be `ChillBot`. |
    | `VMSS_USERNAME` | The username you wish to use to log in to your VMSS via SSH, should you choose to do so. |

## Deploy to Azure

You're ready to deploy!

Navigate to the "Actions" tab on your GitHub.com fork of the repository where you populated your secrets and follow [these instructions](https://docs.github.com/actions/managing-workflow-runs/manually-running-a-workflow#running-a-workflow-on-github) to run the "Deploy to Azure" workflow.

The deployment should take 3-5 minutes and the VMSS will require an additional 10-15 minutes to perform an initial bootstrap and install the dependencies from the [cloud-int file](./.deploy/cloud-init.yml). Subsequent deployments will be much faster since the dependencies only need to be installed once.

Once the initial bootstrapping is complete, your Chill Bot should start running automatically and show as available in Discord.

> Remember to populate your Azure Storage Account with the guild information as described in the [Set Up Your "Database"](#set-up-your-database) section above. Your guild files should be placed in a container named "guilds" within your storage account.
