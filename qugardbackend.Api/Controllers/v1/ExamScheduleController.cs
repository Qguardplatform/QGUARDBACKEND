using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using examportal.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class ExamScheduleController : BaseController
    {
        private readonly IExamScheduleService _examScheduleService;

        public ExamScheduleController(IExamScheduleService examScheduleService)
        {
            _examScheduleService = examScheduleService;
        }

        /// <summary>
        /// Endpoint to create a new exam schedule and optionally create questions under it
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>

        [HttpPost("create")]
        public async Task<IActionResult> CreateQuestion([FromBody] ExamScheduleCreateDto model)
        {
            var response = await _examScheduleService.CreateAsync(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        /// <summary>
        /// Get all the Exams Schedule
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("all-exams-schedules")]
        public async Task<IActionResult> GetAllQuestions([FromQuery] ExamSchedulenFilterModel query)
        {
            var response = await _examScheduleService.GetAllAsync(query);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Get all the Exams Schedule by timing for a specific candidate
        /// </summary>
        /// <param name="candidateUserId"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("all-a-candidate-exams-schedule/{candidateUserId}")]
        public async Task<IActionResult> GetAllExamScheduledByTimingAsync(string candidateUserId, [FromQuery] ExamSchedulenDashboardFilterModel query)
        {
            var response = await _examScheduleService.GetAllExamScheduledByTimingAsync(query, candidateUserId);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Get the details of an exam schedule by exam schedule Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetScheduleById(long id)
        {
            var response = await _examScheduleService.GetByIdAsync(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        /// <summary>
        /// update and Exam Schedule
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateExamSchedule(long id, [FromBody] ExamScheduleCreateDto model)
        {
            var response = await _examScheduleService.UpdateAsync(id, model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Change the status of an exam schedule
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("change-exam-schedule-status")]
        public async Task<IActionResult> ChangeExamScheduleStatus([FromForm] changeExamScheduleStatus model)
        {
            var response = await _examScheduleService.ChangeExamScheduleStatus(model.Id, model.status, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Soft delete an Exam Schedule
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteQuestion(long id)
        {
            var response = await _examScheduleService.DeleteAsync(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
