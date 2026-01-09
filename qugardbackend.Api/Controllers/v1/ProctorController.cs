using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using examportal.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class ProctorController : BaseController
    {   
        private readonly IProctorService _proctorService;
        private readonly ILogger<ProctorController> _logger;

        public ProctorController(
            IProctorService proctorService,
            ILogger<ProctorController> logger)
        {
            _proctorService = proctorService;
            _logger = logger;
        }

        /// <summary>
        /// Webhook to receive events
        /// </summary>
        /// <param name="payload">Proctor flag payload</param>
        [AllowAnonymous]
        [HttpPost("flag")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReceiveProctorEvent([FromBody] ProctorWebhookPayload payload)
        {
            var response = await _proctorService.ProcessProctorFlagAsync(payload);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll([FromQuery] QueryModelMini search)
        {
            var response = await _proctorService.GetProctoringDashboardAsync(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("exam-details/{examScheduleId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByExamSchedule([FromRoute]long examScheduleId, [FromQuery] QueryModelMini search)
        {
            var response = await _proctorService.GetProctoringExamDetailsAsync(examScheduleId, search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("candidate-flags-in-exams/{candidateId}/{examScheduleId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCandidateFlagsInExam([FromRoute] long candidateId, [FromRoute] long examScheduleId)
        {
            var response = await _proctorService.GetCandidateProctorDetailsAsync(candidateId, examScheduleId);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("action")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProctorAction([FromBody] ProctorActionRequest request)
        {
            var response = await _proctorService.ProcessProctorActionAsync(
                request.CandidateId,
                request.ExamScheduleId,
                request.ActionType
            );

            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}