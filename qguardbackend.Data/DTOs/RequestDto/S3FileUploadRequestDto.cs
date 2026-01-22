namespace qguardbackend.Shared.DTOs.RequestDtos;

public class S3FileUploadRequestDto
{
    public string? EntityName { get; set; }
    public string? CategoryName { get; set; }
    public string? FileName { get; set; }
    public Stream FileStream { get; set; }
}

public class S3FileUploadRequestDtoV2
{
    public string? EntityName { get; set; }
    public string? CategoryName { get; set; }
    public string? FileName { get; set; }
    public string? FileString { get; set; }
}

//public class UploadedFileDetails
//{
//    public string fileName { get; set; }
//    public string UploadedFileURL { get; set; }
//    public string UploadedKey { get; set; }
//    public long FileSize { get; set; }
//    public string Category { get; set; }
//    public string FolderPath { get; set; }
//    public string Note { get; set; }
//}
