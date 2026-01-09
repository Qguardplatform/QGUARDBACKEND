using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using examportal.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("institution-admin-summary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<DashboardResponse>>))]
        public async Task<IActionResult> InstitutionAdminDashBoard([FromQuery] DashboardFilterModel search)
        {
            var response = await _dashboardService.GetInstitutionAdminDashboardAsync(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("institution-admin-graph")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<ExamPerformanceDto>>))]
        public async Task<IActionResult> InstitutionAdminGraph([FromQuery] DashboardFilterModel search)
        {
            var response = await _dashboardService.GetInstitutionExamPerformanceAsync(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("admin-summary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<AdminDashboardResponse>>))]
        public async Task<IActionResult> AdminDashBoard([FromQuery] DashboardFilterModel search)
        {
            var response = await _dashboardService.GetAdminDashboardAsync(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}