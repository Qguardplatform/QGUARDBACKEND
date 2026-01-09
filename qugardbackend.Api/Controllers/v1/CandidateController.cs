using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;
using examportal.Api.Controllers;
using examportal.Filter;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class CandidateController : BaseController
    {
        private readonly ICandidateService _candidateService;
        private readonly IInstitutionService _institutionService;

        public CandidateController(ICandidateService candidateService,
            IInstitutionService institutionService)
        {
            _candidateService = candidateService;
            _institutionService = institutionService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<CreateUpdateCandidateResponseDto>>))]
        public async Task<IActionResult> Create([FromForm] CandidateRequestDto model)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var response = await _candidateService.Create(model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }


        [HttpPut("update-candidate/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<CreateUpdateCandidateResponseDto>>))]
        public async Task<IActionResult> Update([FromRoute] long id, [FromForm] UpdateCandidateRequestDto model)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var response = await _candidateService.Update(id, model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// migrate candidates to new session, semester, and level to inherit the exams
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("migrate-candidates")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<CreateUpdateCandidateResponseDto>>))]
        public async Task<IActionResult> UpdateCandidateState(UpdateCandidateStatusRequestDto model)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var response = await _candidateService.UpdateCandidateState(model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
      
        [HttpGet("get-candidate-details/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<CandidateModel>>))]
        public async Task<IActionResult> GetById([FromRoute] long id)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var response = await _candidateService.GetById(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("get-candidate-details-by-userid/{candidateUserId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<CandidateModel>>))]
        public async Task<IActionResult> GetCandidateDetailByUserId([FromRoute] string candidateUserId)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var response = await _candidateService.GetCandidateDetailByUserId(candidateUserId);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("candidate-profile-preview")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<CandidatePreviewModel>>))]
        public async Task<IActionResult> GetCandidateDetailByUserId()
        {
            var response = await _candidateService.CandidateProfilePreview(CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("delete/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<bool>>))]
        public async Task<IActionResult> Delete([FromRoute] long id)
        {
            var response = await _candidateService.Delete(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("change-status/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<bool>>))]
        public async Task<IActionResult> ChangeStatus( long id)
        {
            var response = await _candidateService.EnableDisableCandidate(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("all-candidates")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<CandidateModel>>>))]
        public async Task<IActionResult> AllStudentsInInstitution([FromQuery] CandidateFilterModel search)
        {
            var response = await _candidateService.GetAllCandidates(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("candidate-by-institutionId/{id:long}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<CandidateModel>>>))]
        public async Task<IActionResult> GetStudentsByInstitutionId([FromRoute]long id, [FromQuery] CandidateFilterModel search)
        {
            var response = await _candidateService.GetCandidatesByInstitutionId(id, search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("candidate-by-institution-code/{code}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<CandidateModel>>>))]
        public async Task<IActionResult> GetStudentsByInstitutionCode([FromRoute] string code, [FromQuery] CandidateFilterModel search)
        {
            var response = await _candidateService.GetCandidatesByInstitutionCode(code, search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("template-download")]
        public async Task<IActionResult> DownloadCandidateTemplate()
        {
            var result = await _candidateService.GetDownloadFormat();

            if (!result.IsSuccess)
                return BadRequest(result);

            var csvBuilder = new StringBuilder();

            foreach (var row in result.Data)
            {
                csvBuilder.AppendLine(string.Join(",", row.Select(x => $"\"{x}\"")));
            }

            var bytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var output = new MemoryStream(bytes);

            return File(output, "text/csv", "candidate_upload_template.csv");
        }

        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUploadCandidates([ModelBinder(typeof(JsonWithFilesFormDataModelBinder))] CandidateUploadModel model)
        {
            var response = await _candidateService.BulkUploadCandidatesAsync(model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Spool all candidates
        /// </summary>
        [HttpGet("export-candidate-data")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<ExportStudentExamsDto>>))]
        public async Task<IActionResult> ExportExam([FromQuery] CandidateExportModel search)
        {
            var response = await _candidateService.ExportCandidates(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}