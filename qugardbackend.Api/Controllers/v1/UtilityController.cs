
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using qguardbackend.Api.ServiceExtensions;
using qguardbackend.Application.Interfaces;
using qguardbackend.Application.Services;
using qguardbackend.Data.Enums;

namespace qguardbackend.Api.Controllers.v2;

[ApiVersion("1.0")]
[Produces("application/json")]
[AllowAnonymous]
public class UtilityController : BaseController
{
    private readonly IUtilityService _utilityService;

    public UtilityController(IUtilityService utilityService)
    {
        _utilityService = utilityService;
    }

    /// <summary>
    /// upload multiple files without notes
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="filetype"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    [HttpPost, Route("upload-multiple-files")]
    public async Task<IActionResult> UploadMultipleFile(EntityEnum entity, FileTypeEnum filetype,    IFormFileCollection files)
    {

        if (files == null || files.Count == 0)
        {
            return BadRequest("No files uploaded");
        }

        var result = await _utilityService.UploadMultipleFilesToS3(entity.GetEnumText(), filetype.GetEnumText(), files);
    
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// upload multiple files with notes, each note is for each file respectively
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="filetype"></param>
    /// <param name="files"></param>
    /// <param name="notes"></param>
    /// <returns></returns>
    //[HttpPost, Route("upload-multiple-files")]
    //public async Task<IActionResult> UploadMultipleFilesWithNotes(
    //[FromForm] EntityEnum entity,
    //[FromForm] FileTypeEnum filetype,
    //[FromForm] IFormFileCollection files,
    //[FromForm] List<string>? notes)
    //{
    //    if (files == null || files.Count == 0)
    //        return BadRequest("No files uploaded");

    //    if (notes != null && notes.Count != files.Count)
    //        return BadRequest("Number of notes must match number of files");

    //    var result = await _utilityService.UploadMultipleFilesToS3WithNotes(
    //        entity.GetEnumText(),
    //        filetype.GetEnumText(),
    //        files,
    //        notes);

    //    return result.IsSuccess ? Ok(result) : BadRequest(result);
    //}


    [HttpPost, Route("upload-file")]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileRequestDto model)
    {
        if (!ModelState.IsValid)
            return UnprocessableEntity(GetValidationErrors<bool>(ModelState));


        if (model.File == null || model.File.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using var fileStream = model.File.OpenReadStream();
        var result = await _utilityService.UploadFileToS3(new UploadFileRequestDataDto
        {
            FileStream = fileStream,
            Filename = model.Filename,
            FileContentType = model.File.ContentType,
            FileExtension = Path.GetExtension(model.File.FileName),
        });
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }


}
