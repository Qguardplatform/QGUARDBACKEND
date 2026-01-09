using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using examportal.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class SessionController : BaseController
    {
        private readonly ISessionService _sessionService;

        public SessionController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<SessionDto>>))]
        public async Task<IActionResult> Create([FromBody] SessionCreateModel model)
        {
            var response = await _sessionService.Create(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<SessionDto>>>))]
        public async Task<IActionResult> GetAll([FromQuery] QueryModelMini search)
        {
            var response = await _sessionService.GetAll(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<SessionDto>>))]
        public async Task<IActionResult> Get([FromRoute] long id)
        {
            var response = await _sessionService.GetById(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<SessionDto>>))]
        public async Task<IActionResult> Update([FromRoute] long id, [FromBody] SessionCreateModel model)
        {
            var response = await _sessionService.Update(id, model, CurrentUser.Email);
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
            var response = await _sessionService.Delete(id, CurrentUser.Email);
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
            var response = await _sessionService.EnableDisableLevel(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
