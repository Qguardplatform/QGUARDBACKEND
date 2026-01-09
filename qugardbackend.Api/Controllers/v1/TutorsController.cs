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
    public class TutorsController : BaseController
    {
        private readonly ITutorsService _tutorsService;

        public TutorsController(ITutorsService tutorsService)
        {
            _tutorsService = tutorsService;
        }
        
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<ProgramModel>>>))]
        public async Task<IActionResult> GetAll([FromQuery] QueryModelMini search)
        {
            var response = await _tutorsService.GetAll(search);
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
            var response = await _tutorsService.GetById(id);
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
            var response = await _tutorsService.EnableDisableLevel(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
