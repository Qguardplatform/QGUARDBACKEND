using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using examportal.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class InstitutionController : BaseController
    {
        private readonly IInstitutionService _institutionService;

        public InstitutionController(IInstitutionService institutionService)
        {
            _institutionService = institutionService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<InstitutionResponseDto>>))]
        public async Task<IActionResult> Create([FromForm] InstitutionCreateModel model)
        {
            var response = await _institutionService.Create(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<InstitutionResponseDto>>>))]
        public async Task<IActionResult> GetAll([FromQuery] InstitutionFilterModel search)
        {
            var response = await _institutionService.GetAll(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("by-code/{code}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<InstitutionResponseDto>>))]
        public async Task<IActionResult> GetByCode([FromRoute] string code)
        {
            var response = await _institutionService.GetByCode(code);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        [HttpGet("by-hostname/{hostname}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<InstitutionResponseDto>>))]
        public async Task<IActionResult> GetByHostname([FromRoute] string hostname)
        {
            var response = await _institutionService.GetByHostName(hostname);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<InstitutionResponseDto>>))]
        public async Task<IActionResult> GetById([FromRoute] long id)
        {
            var response = await _institutionService.GetById(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<InstitutionResponseDto>>))]
        public async Task<IActionResult> Update([FromRoute] long id, [FromForm] InstitutionCreateModel model)
        {
            var response = await _institutionService.Update(id, model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<string>>))]
        public async Task<IActionResult> Delete([FromRoute] long id)
        {
            var response = await _institutionService.Delete(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("change-status/{institutionId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<bool>>))]
        public async Task<IActionResult> ChangeStatus(long institutionId)
        {
            var response = await _institutionService.ChangeInstitutionStatus(institutionId, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
