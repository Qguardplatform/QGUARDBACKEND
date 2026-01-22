using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using qguardbackend.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Shared.DTOs.RequestDtos;
using qguardbackend.Application.Services;
using Amazon;

namespace qguardbackend.Core.Services
{
    public class S3Service : IS3Service
    {
        public static readonly string _s3Name = "AmazonS3";
        public static readonly string _Region = "Region";
        //public static readonly string _SecretKey = "SecretKey";
        public static readonly string _AccessKey2 = "AccessKey";

        //public static readonly string _bucketName = "BucketName";
        private readonly ISettingsService _settingsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<S3Service> _logger;

        private readonly string _region;
        private readonly string _SecretKey;
        private readonly string _Accesskey;
        private readonly string _bucketName;
        private readonly AWS _awsOptions;
        private readonly IAmazonS3 _s3Client;

        public S3Service(ISettingsService settingsService,
            IConfiguration configuration, 
            IOptions<AWS> awsOptions,
            ILogger<S3Service> logger)
        {
            _settingsService = settingsService;
            _logger = logger;


            _logger = logger;
            _awsOptions = awsOptions.Value;
            _region = _awsOptions.Region;
            _bucketName = _awsOptions.BucketName;
            //_Accesskey = Environment.GetEnvironmentVariable("S3AccessKey");
            _Accesskey = _configuration["S3Params:S3AccessKey"];
            //_SecretKey = Environment.GetEnvironmentVariable("S3SecretKey");
            _SecretKey = _configuration["S3Params:S3SecretKey"];
            _s3Client = new AmazonS3Client(new BasicAWSCredentials(_Accesskey, _SecretKey),
                new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_region) });

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
                var credentials = await this.GetParams(_s3Name, _AccessKey2, _SecretKey, _bucketName, _Region);
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
                var credentials = await this.GetParams(_s3Name, _AccessKey2, _SecretKey, _bucketName, _Region);
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
                }
                ;

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


        //-------------------------------


        public async Task<UploadedFileDetails> UploadFileGetDetailsAsync(long size, Stream fileStream, string fileName, string contentType, string extension)
        {
            try
            {

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = fileStream,
                    Key = $"{fileName.Replace(" ", "_")}-{Guid.NewGuid()}{extension}",
                    BucketName = _bucketName,
                    ContentType = contentType,
                    //CannedACL = S3CannedACL.PublicRead // Makes the file publicly accessible
                };

                using var transferUtility = new TransferUtility(_Accesskey, _SecretKey, RegionEndpoint.USEast1);
                await transferUtility.UploadAsync(uploadRequest);

                string fileUrl = $"https://{_awsOptions.BucketName}.s3.amazonaws.com/{fileName.Replace(" ", "_")}{extension}";

                var resp = new UploadedFileDetails
                {
                    UploadedFileURL = fileUrl,
                    UploadedKey = uploadRequest.Key,
                    FileSize = size,
                    fileName = $"{fileName}{extension}"
                };

                return resp;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(S3Service).Name,
                    nameof(UploadFileAsync));

                throw new Exception($"S3 file upload failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(S3Service).Name,
                    nameof(UploadFileAsync));

                throw new Exception($"S3 file upload failed: {ex.Message}");
            }
        }
        public async Task<UploadedFileDetails> UploadFileGetDetailsAsync(string entity, string filetype, long size, Stream fileStream, string fileName, string contentType, string extension)
        {
            try
            {

                // Clean filename and create unique key with folder structure
                var cleanFileName = fileName.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
                var uniqueKey = $"{entity}/{filetype}/{cleanFileName}-{Guid.NewGuid()}{extension}";

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = fileStream,
                    Key = uniqueKey,
                    BucketName = _bucketName,
                    ContentType = contentType,
                    //CannedACL = S3CannedACL.PublicRead // Makes the file publicly accessible
                };

                using var transferUtility = new TransferUtility(_Accesskey, _SecretKey, RegionEndpoint.USEast1);
                await transferUtility.UploadAsync(uploadRequest);

                // Generate the correct S3 URL with the folder structure
                string fileUrl = $"https://{_bucketName}.s3.amazonaws.com/{uniqueKey}";

                var resp = new UploadedFileDetails
                {
                    UploadedFileURL = fileUrl,
                    UploadedKey = uniqueKey,
                    FileSize = size,
                    fileName = $"{cleanFileName}{extension}",
                    Category = entity,
                    FolderPath = $"{entity}/{filetype}/"
                };

                return resp;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(S3Service).Name,
                    nameof(UploadFileGetDetailsAsync));
                throw new Exception($"S3 file upload failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(S3Service).Name,
                    nameof(UploadFileGetDetailsAsync));
                throw new Exception($"S3 file upload failed: {ex.Message}");
            }
        }



        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string extension)
        {
            try
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = fileStream,
                    Key = $"{fileName.Replace(" ", "_")}{extension}",
                    BucketName = _bucketName,
                    ContentType = contentType,
                    //CannedACL = S3CannedACL.PublicRead // Makes the file publicly accessible
                };

                using var transferUtility = new TransferUtility(_Accesskey, _SecretKey, RegionEndpoint.USEast1);
                await transferUtility.UploadAsync(uploadRequest);

                string fileUrl = $"https://{_awsOptions.BucketName}.s3.amazonaws.com/{fileName.Replace(" ", "_")}{extension}";
                return fileUrl;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(S3Service).Name,
                    nameof(UploadFileAsync));

                throw new Exception($"S3 file upload failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(S3Service).Name,
                    nameof(UploadFileAsync));

                throw new Exception($"S3 file upload failed: {ex.Message}");
            }
        }

        public async Task<(string, string)> UploadFileAsyncV2(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = fileStream,
                    Key = fileName,
                    BucketName = _bucketName,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.PublicRead // Makes the file publicly accessible
                };

                using var transferUtility = new TransferUtility(_s3Client);

                await transferUtility.UploadAsync(uploadRequest);

                string fileUrl = $"https://{_bucketName}.s3.amazonaws.com/{fileName}";
                return (fileUrl, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(S3Service).Name,
                    nameof(UploadFileAsync));

                throw new Exception($"S3 file upload failed: {ex.Message}");
            }
        }

        public async Task<string> UploadFileStreamAsync(S3FileUploadRequestDto model)
        {
            var folderPrefix = $"{model.EntityName}/{model.CategoryName}/";

            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = folderPrefix,
                MaxKeys = 1
            };

            var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

            if (!listResponse.S3Objects.Any())
            {
                var placeholderRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = folderPrefix,
                    ContentBody = string.Empty
                };

                await _s3Client.PutObjectAsync(placeholderRequest);
            }

            var objectKey = $"{folderPrefix}{model.FileName}";

            var uploadRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = model.FileStream,
                ContentType = "application/octet-stream",
                CannedACL = S3CannedACL.PublicRead
            };

            await _s3Client.PutObjectAsync(uploadRequest);
            return $"https://{_bucketName}.s3.{_region}.amazonaws.com/{objectKey}";
        }

        public async Task<string> UploadBase64FileAsync(S3FileUploadRequestDtoV2 model)
        {
            var base64Data = model.FileString;

            if (base64Data.Contains(","))
                base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);

            string contentType = Helper.ExtractMimeType(model.FileString);
            var fileBytes = Convert.FromBase64String(base64Data);
            var folderPrefix = $"{model.EntityName}/{model.CategoryName}/";

            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = folderPrefix,
                MaxKeys = 1
            };

            var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

            if (!listResponse.S3Objects.Any())
            {
                var placeholderRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = folderPrefix,
                    ContentBody = string.Empty
                };

                await _s3Client.PutObjectAsync(placeholderRequest);
            }

            var objectKey = $"{folderPrefix}{model.FileName}";

            using var stream = new MemoryStream(fileBytes);

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = stream,
                ContentType = contentType,
                //CannedACL = S3CannedACL.PublicRead
            };

            await _s3Client.PutObjectAsync(putRequest);
            return $"https://{_bucketName}.s3.{_region}.amazonaws.com/{objectKey}";
        }


        public async Task<Stream?> GetFileStreamAsync(string filePath, string key)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = filePath,
                    //BucketName = _bucketName,
                    //Prefix = folderPrefix,
                };

                //using var response = await _s3Client.GetObjectAsync(request);
                using var response = await _s3Client.GetObjectAsync(_bucketName, key);

                // Copy response stream to memory stream to avoid disposal when exiting using block
                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reset to beginning

                return memoryStream;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // File not found in S3
                return null;
            }


        }

        public async Task<S3uploadResponse> UploadBase64FileGetKeyAsync(S3FileUploadRequestDtoV2 model)
        {
            var resp = new S3uploadResponse();
            var base64Data = model.FileString;

            if (base64Data.Contains(","))
                base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);

            string contentType = Helper.ExtractMimeType(model.FileString);
            var fileBytes = Convert.FromBase64String(base64Data);
            var folderPrefix = $"{model.EntityName}/{model.CategoryName}/";

            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = folderPrefix,
                MaxKeys = 1
            };

            var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

            if (!listResponse.S3Objects.Any())
            {
                var placeholderRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = folderPrefix,
                    ContentBody = string.Empty
                };

                await _s3Client.PutObjectAsync(placeholderRequest);
            }

            var objectKey = $"{folderPrefix}{model.FileName}";

            using var stream = new MemoryStream(fileBytes);

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = stream,
                ContentType = contentType,
                //CannedACL = S3CannedACL.PublicRead
            };

            await _s3Client.PutObjectAsync(putRequest);
            resp.URL = $"https://{_bucketName}.s3.{_region}.amazonaws.com/{objectKey}";
            resp.Key = objectKey;
            return resp;
        }



    }

    public class S3uploadResponse
    {
        public string URL { get; set; }
        public string Key { get; set; }
    }

    public class UploadFileRequestDto
    {
        public string Filename { get; set; }
        public IFormFile File { get; set; }
    }

    //public class UploadFileRequestDataDto
    //{
    //    public string Filename { get; set; }
    //    public string FileContentType { get; set; }
    //    public string FileExtension { get; set; }
    //    public Stream FileStream { get; set; }
    //}
}
