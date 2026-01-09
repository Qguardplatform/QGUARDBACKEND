using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Enums;
using examportal.Api.Controllers;
using examportal.Api.ServiceExtensions;
using LS1_Backend.LS1.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class GradesController : BaseController
    {
        private readonly IExamService _examService;

        public GradesController(IExamService examService)
        {
            _examService = examService;
        }

        [HttpGet("all-department-result-submissions-by-exam-schedule")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<ScheduleExamQuestionListModel>>>))]
        public async Task<IActionResult> AllDepartmentResultSubmissionExam([FromQuery] DepartmeentFacultyScheduleExamQuestionFilterModel search)
        {
            var response = await _examService.AllDepartmentResultSubmissionExam(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("all-result-submissions-by-exam-schedule")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<ScheduleExamQuestionListModel>>>))]
        public async Task<IActionResult> SubmissionExam([FromQuery] ScheduleExamQuestionFilterModel search)
        {
            var response = await _examService.AllResultSubmissionExam(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("get-result-by-exam-schedule/{examScheduleId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<SingleExamResultListModel>>))]
        public async Task<IActionResult> ResultByCourse([FromRoute] long examScheduleId, [FromQuery] ExamRecordsByDepartmentFilterModel search)
        {
            var response = await _examService.SingleExamRecordsByCourse(examScheduleId, search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }       
        
        /// <summary>
        /// endpoint to publish result
        /// </summary>
        /// <param name="examScheduleId"></param>
        /// <returns></returns>
        [HttpPut("publish-and-unpublished-result/{examScheduleId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<string>>))]
        public async Task<IActionResult> ChangeStatus(long examScheduleId)
        {
            var response = await _examService.PublishResult(examScheduleId, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// enable resit or resubmission for a candidate for an exam schedule
        /// </summary>
        /// <param name="examScheduleId"></param>
        /// <param name="candidateId"></param>
        /// <returns></returns>
        [HttpPut("allow-candidate-exam-resit/{examScheduleId}/{candidateId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<string>>))]
        public async Task<IActionResult> AllowCandidateExamResit(long examScheduleId, long candidateId)
        {
            var response = await _examService.AllowCandidateExamResit(examScheduleId, candidateId, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("allow-multiple-candidate-exam-resit")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<string>>))]
        public async Task<IActionResult> AllowMultipleCandidatesExamResit(AllowMultipleCandidatesExamResitRequestDto
            request)      {
            var response = await _examService.AllowMultipleCandidatesExamResit(request, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}