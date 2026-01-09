using Amazon.S3.Model;
using Amazon.S3;
using qguardbackend.Core.Interfaces;
using qguardbackend.Data.DTOs;
using Microsoft.AspNetCore.Http;
using qguardbackend.Data.Constants;
using Microsoft.Extensions.Logging;
using Amazon.S3.Transfer;

namespace qguardbackend.Core.Services
{
    public class S3Service : IS3Service
    {
        public static readonly string _s3Name = "AmazonS3";
        public static readonly string _Region = "Region";
        public static readonly string _SecretKey = "SecretKey";
        public static readonly string _AccessKey = "AccessKey";
        public static readonly string _bucketName = "BucketName";
        private readonly ISettingsService _settingsService;
        private readonly ILogger<S3Service> _logger;

        public S3Service(ISettingsService settingsService, ILogger<S3Service> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        private async Task<(bool, string, string, string, string)> GetParams(string S3Name, string accessKey, string secretKey, string bucket, string region)
        {
            var AccessKey = await _settingsService.GetConfigurationSettings(S3Name, accessKey);
            if (AccessKey.ResponseCode != ResponseCodes.SuccessCode) return (false, AccessKey.Message, null, null, null);
            var SecretKey = await _settingsService.GetConfigurationSettings(S3Name, secretKey);
            if (SecretKey.ResponseCode != ResponseCodes.SuccessCode) return (false, SecretKey.Message, null, null, null);
            var Region = await _settingsService.GetConfigurationSettings(S3Name, region);
            if (Region.ResponseCode != ResponseCodes.SuccessCode) return (false, Region.Message, null, null, null);
            var bucketName = await _settingsService.GetConfigurationSettings(S3Name, bucket);
            if (bucketName.ResponseCode != ResponseCodes.SuccessCode) return (false, bucketName.Message, null, null, null);

            return (true, AccessKey.Data, SecretKey.Data, Region.Data, bucketName.Data);
        }

        public async Task<FileUploadResponse> DeleteFileFromS3Async(string filePath)
        {
            try
            {
                var credentials = await this.GetParams(_s3Name, _AccessKey, _SecretKey, _bucketName, _Region);
                if (credentials.Item1 == false) throw new Exception($"{credentials.Item2}");

                var client = new AmazonS3Client(credentials.Item2, credentials.Item3, Amazon.RegionEndpoint.USEast1);

                var request = new DeleteObjectRequest
                {
                    BucketName = credentials.Item5,
                    Key = filePath
                };

                var response = await client.DeleteObjectAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    return new FileUploadResponse
                    {
                        Success = true,
                        Message = response.HttpStatusCode.ToString(),
                        FileName = filePath
                    };
                }
                else if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return new FileUploadResponse
                    {
                        Success = true,
                        Message = response.HttpStatusCode.ToString(),
                        FileName = filePath
                    };
                }
                else
                {
                    return new FileUploadResponse
                    {
                        Success = false,
                        FileName = filePath,
                        Message = "Failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception($"{ex.Message}");
            }
        }

        public async Task<FileUploadResponse> UploadToS3Async(IFormFile file, string fileName)
        {
            try
            {
                var credentials = await this.GetParams(_s3Name, _AccessKey, _SecretKey, _bucketName, _Region);
                if (credentials.Item1 == false)
                {
                    return new FileUploadResponse
                    {
                        Success = false,
                        FileName = credentials.Item2
                    };
                }
                // connecting to the client
                var client = new TransferUtility(credentials.Item2, credentials.Item3, Amazon.RegionEndpoint.USEast1);

                // get the file and convert it to the byte[]
                byte[] fileBytes = new Byte[file.Length];
                file.OpenReadStream().Read(fileBytes, 0, Int32.Parse(file.Length.ToString()));

                using (var stream = new MemoryStream(fileBytes))
                {
                    var request = new TransferUtilityUploadRequest
                    {
                        BucketName = credentials.Item5,
                        Key = fileName,
                        InputStream = stream,
                        ContentType = file.ContentType,
                        CannedACL = S3CannedACL.PublicRead,
                        StorageClass = S3StorageClass.ReducedRedundancy
                    };
                    await client.UploadAsync(request);
                };

                return new FileUploadResponse
                {
                    Success = true,
                    Message = "Successful",
                    FileName = GetFullUploadedFileName(fileName, credentials.Item5, credentials.Item4)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message); 
                return new FileUploadResponse
                {
                    Success = false,
                    Message = $"{ex.Message}",
                    FileName = null
                };
            }
        }

        private string GetFullUploadedFileName(string filename, string bucket, string region)
        {
            return string.Format("https://{0}.s3.{1}.amazonaws.com/{2}", bucket, region, filename);
        }
    }
}
