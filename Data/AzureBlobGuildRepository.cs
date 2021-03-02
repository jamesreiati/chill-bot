using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reiati.ChillBot.Tools;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// A repository of <see cref="Guild"/> objects to be checked out and checked in from Azure Blob storage.
    /// </summary>
    public class AzureBlobGuildRepository : IGuildRepository
    {
        /// <summary>
        /// The HTTP status code returned when a blob is not found.
        /// </summary>
        private const int NotFoundStatus = 404;

        /// <summary>
        /// The HTTP status code returned when there is a conflict.
        /// </summary>
        private const int ConflictStatus = 409;

        /// <summary>
        /// The maximum amount of time to lease a guild blob when it is checked out.
        /// </summary>
        /// <remarks>
        /// If the guild blob is not returned within this time period, the lease will expire.
        /// </remarks>
        private static readonly TimeSpan BlobLeaseDuration = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The client to access the container in Blob storage
        /// </summary>
        private BlobContainerClient blobContainerClient;

        /// <summary>
        /// Constructs a <see cref="AzureBlobGuildRepository"/>.
        /// </summary>
        public AzureBlobGuildRepository(string connectionString, string containerName)
        {
            ValidateArg.IsNotNullOrWhiteSpace(connectionString, nameof(connectionString));
            ValidateArg.IsNotNullOrWhiteSpace(containerName, nameof(containerName));

            this.blobContainerClient = new BlobContainerClient(connectionString, containerName);
            var ignoreAwait = this.blobContainerClient.CreateIfNotExistsAsync();
        }

        /// <summary>
        /// Checkout out a <see cref="Guild"/>.
        /// </summary>
        /// <param name="guildId">An id representing a guild.</param>
        /// <param name="recycleResult">A preallocated result that should be returned if passed in.</param>
        /// <returns>The borrowed guild.</returns>
        public async Task<GuildCheckoutResult> Checkout(Snowflake guildId, GuildCheckoutResult recycleResult = null)
        {
            var retVal = recycleResult ?? new GuildCheckoutResult();

            try
            {
                string blobName = AzureBlobGuildRepository.GetBlobName(guildId);
                var blobClient = this.blobContainerClient.GetBlockBlobClient(blobName);

                // Aquire a lease on the blob
                var blobLeaseClient = blobClient.GetBlobLeaseClient();
                var leaseResult = await blobLeaseClient.AcquireAsync(AzureBlobGuildRepository.BlobLeaseDuration).ConfigureAwait(false);
                var lease = leaseResult.Value;

                Guild guild;
                using (var sourceStream = await blobClient.OpenReadAsync())
                using (var streamReader = new StreamReader(sourceStream))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    guild = GuildConverter.FromJToken(guildId, await JObject.ReadFromAsync(jsonReader));
                }

                retVal.ToSuccess(new Borrowed<Guild>(
                    isntance: guild,
                    data: lease,
                    onReturn: this.ReturnGuild));

                return retVal;
            }
            catch (RequestFailedException e)
            {
                if (e.Status == AzureBlobGuildRepository.NotFoundStatus)
                {
                    retVal.ToDoesNotExist();
                    return retVal;
                }
                else if (e.Status == AzureBlobGuildRepository.ConflictStatus && "LeaseAlreadyPresent".Equals(e.ErrorCode, StringComparison.OrdinalIgnoreCase))
                {
                    retVal.ToLocked();
                    return retVal;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// The action invoked when a <see cref="Borrowed<T>"/> is being returned.
        /// </summary>
        /// <param name="guild">The guild that's being returned.</param>
        /// <param name="data">
        /// The object placed into the data field at the construction of the <see cref="Borrowed<T>"/>.
        /// </param>
        /// <param name="commit">
        /// True if the user wanted to commit the changes, false if they wanted them discarded.
        /// </param>
        private void ReturnGuild(Guild guild, object data, bool commit)
        {
            string blobName = AzureBlobGuildRepository.GetBlobName(guild.Id);

            var blobLease = (BlobLease)data;
            var blobClient = this.blobContainerClient.GetBlockBlobClient(blobName);

            // Get the lease client and populate the lease information in the request options
            var blobLeaseClient = blobClient.GetBlobLeaseClient(blobLease.LeaseId);
            var blobRequestConditions = new BlobRequestConditions() { LeaseId = blobLeaseClient.LeaseId };

            if (commit)
            {
                JsonSerializer serializer = new JsonSerializer();
#if DEBUG
                serializer.Formatting = Formatting.Indented;
#else
                serializer.Formatting = Formatting.None;
#endif

                using (var stream = blobClient.OpenWrite(overwrite: true, new BlockBlobOpenWriteOptions() { OpenConditions = blobRequestConditions }))
                using (var streamWriter = new StreamWriter(stream))
                using (var writer = new JsonTextWriter(streamWriter))
                {
                    serializer.Serialize(writer, GuildConverter.ToJToken(guild));
                }
            }

            // Release the lease on the blob
            blobLeaseClient.Release();
        }

        /// <summary>
        /// Returns the name of the blob that stores a Guild.
        /// </summary>
        /// <param name="guildId">An id representing a guild.</param>
        /// <returns>The path to the file that stores a Guild.</returns>
        private static string GetBlobName(Snowflake guildId)
        {
            return guildId + ".json";
        }
    }
}
