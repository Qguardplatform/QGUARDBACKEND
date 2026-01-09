using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SISService.BoilerPlate.Service.Interfaces;
using SISService.Core.ConfigModels;

namespace qguardbackend.Core.Services
{
    public class AzureService : IAzureService
    {
        private readonly IHttpContextAccessor _httpCxtAccessor;
        private readonly AzureSetting _azureSetting;

        public AzureService(IHttpContextAccessor httpCxtAccessor, 
            IOptions<AzureSetting> azureSetting)
        {
            _httpCxtAccessor = httpCxtAccessor;
            _azureSetting = azureSetting.Value; 
        }

        public async Task<string> UploadAsync(string blobName, byte[] data)
        {
            // Create a BlobServiceClient object which will be used to create the container client
            BlobServiceClient blobServiceClient = new BlobServiceClient(_azureSetting.BlobConnectionString);

            // Get a reference to the container
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_azureSetting.BlobContainerName);
            // Create the container if it doesn't exist
            await containerClient.CreateIfNotExistsAsync();

            // Get a reference to the blob
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            // Set the content type explicitly
            var contentType = "image/jpeg";
            // Open the file and upload the data to Azure Storage
            using (MemoryStream stream = new MemoryStream(data))
            {

                // Create BlobHttpHeaders to set the content type
                BlobHttpHeaders blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType // Set the content type
                };
                // await blobClient.UploadAsync(stream, true);
                await blobClient.UploadAsync(stream, true);
                blobClient.SetHttpHeaders(blobHttpHeaders);
            }
            return blobClient.Name;
        }
    }
}

