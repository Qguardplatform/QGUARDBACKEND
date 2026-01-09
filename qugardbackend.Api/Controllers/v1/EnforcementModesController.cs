using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using examportal.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class EnforcementModesController : BaseController
    {
        private readonly IEnforcementModeService _enforcementModeService;

        public EnforcementModesController(IEnforcementModeService enforcementModeService)
        {
            _enforcementModeService = enforcementModeService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<EnforcementModeModel>>))]
        public async Task<IActionResult> Create([FromBody] EnforcementModeCreateModel model)
        {
            var response = await _enforcementModeService.Create(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<EnforcementModeModel>>>))]
        public async Task<IActionResult> GetAll([FromQuery] QueryModelMini search)
        {
            var response = await _enforcementModeService.GetAll(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<EnforcementModeModel>>))]
        public async Task<IActionResult> Get([FromRoute] long id)
        {
            var response = await _enforcementModeService.GetById(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<EnforcementModeModel>>))]
        public async Task<IActionResult> Update([FromRoute] long id, [FromBody] EnforcementModeCreateModel model)
        {
            var response = await _enforcementModeService.Update(id, model, CurrentUser.Email);
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
            var response = await _enforcementModeService.Delete(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}