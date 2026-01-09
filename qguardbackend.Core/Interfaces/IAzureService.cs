namespace SISService.BoilerPlate.Service.Interfaces
{
    public interface IAzureService
    {
        //Task<ResultModel<string>> AzureBlobUpload(IFormFile profileImage,
        //           AzureBlobFolderName uploadType);

        Task<string> UploadAsync(string blobName, byte[] data);
    }
}
