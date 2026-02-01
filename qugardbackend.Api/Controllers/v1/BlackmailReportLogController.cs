using Microsoft.AspNetCore.Mvc;
using qguardbackend.Api.Controllers;
using qguardbackend.Core.Interfaces;
using qguardbackend.Core.Services;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;

namespace qguardbackend.Controllers.v1
{
    [ApiVersion("1.0")]
    public class BlackmailReportLogController : BaseController
    {
        private readonly IBlackmailReportlogService _reportService;

        public BlackmailReportLogController(IBlackmailReportlogService reportService)
        {
            _reportService = reportService;
        }



        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomResult<BlackmailReportLogResponseDto>))]
        public async Task<IActionResult> Create([FromBody] BlackmailReportLogsRequestDto model)
        {
            if (!ModelState.IsValid) return BadRequest(GetModelStateErrors(ModelState));
            var response = await _reportService.Create(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }


        [HttpPost("update-report-log/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomResult<BlackmailReportLogResponseDto>))]
        public async Task<IActionResult> Update(long id,[FromBody] BlackmailReportLogsRequestDto model)
        {
            if (!ModelState.IsValid) return BadRequest(GetModelStateErrors(ModelState));
            var response = await _reportService.Update(id,model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }


        [HttpGet("get-all-report-log")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<BlackmailReportLogResponseDto>>>))]
        public async Task<IActionResult> GetAll([FromQuery] QueryModelMini filter)
        {
            var response = await _reportService.GetAll(filter);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("get-by-id/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomResult<BlackmailReportLogResponseDto>))]
        public async Task<IActionResult> GetById([FromRoute] long id)
        {
            var response = await _reportService.GetById(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("delete-report-log/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<string>))]
        public async Task<IActionResult> Delete([FromRoute] long id)
        {
            var response = await _reportService.Delete(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }




    
    }
}