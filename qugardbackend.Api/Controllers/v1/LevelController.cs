using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using Microsoft.AspNetCore.Mvc;
using examportal.Api.Controllers;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class LevelController : BaseController
    {
        private readonly ILevelService _levelService;

        public LevelController(ILevelService levelService)
        {
            _levelService = levelService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<LevelModel>>))]
        public async Task<IActionResult> Create([FromBody] LevelCreateModel model)
        {
            var response = await _levelService.Create(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<LevelModel>>>))]
        public async Task<IActionResult> GetAll([FromQuery] QueryModelMini search)
        {
            var response = await _levelService.GetAll(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<LevelModel>>))]
        public async Task<IActionResult> Get([FromRoute] long id)
        {
            var response = await _levelService.GetById(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<LevelModel>>))]
        public async Task<IActionResult> Update([FromRoute] long id, [FromBody] LevelCreateModel model)
        {
            var response = await _levelService.Update(id, model, CurrentUser.Email);
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
            var response = await _levelService.EnableDisableLevel(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<string>>))]
        public async Task<IActionResult> Delete( long id)
        {
            var response = await _levelService.Delete(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
