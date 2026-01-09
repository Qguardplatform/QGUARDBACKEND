using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using examportal.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class ProgramController : BaseController
    {
        private readonly IProgramService _programService;
        public ProgramController(IProgramService programService)
        {
            _programService = programService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<ProgramModel>>))]
        public async Task<IActionResult> Create([FromBody] ProgramCreateModel model)
        {
            var response = await _programService.Create(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<ProgramModel>>>))]
        public async Task<IActionResult> GetAll([FromQuery] GetProgramQueryModelMini search)
        {
            var response = await _programService.GetAll(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<ProgramModel>>))]
        public async Task<IActionResult> Get([FromRoute] long id)
        {
            var response = await _programService.GetById(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<ProgramModel>>))]
        public async Task<IActionResult> Update([FromRoute] long id, [FromBody] ProgramCreateModel model)
        {
            var response = await _programService.Update(id, model, CurrentUser.Email);
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
            var response = await _programService.Delete(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        [HttpPut("enable-and-disabled/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<string>>))]
        public async Task<IActionResult> ChangeStatus( long id)
        {
            var response = await _programService.EnableDisableLevel(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
