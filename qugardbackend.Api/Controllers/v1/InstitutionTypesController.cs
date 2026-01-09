using edutech.services.examportal.Core.Interfaces;
using edutech.services.examportal.Data.Constants;
using edutech.services.examportal.Data.DTOs.Results;
using edutech.services.examportal.Data.DTOs;
using examportal.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class InstitutionTypesController : BaseController
    {
        private readonly IInstitutionTypeService _institutionTypeService;

        public InstitutionTypesController(IInstitutionTypeService institutionTypeService)
        {
            _institutionTypeService = institutionTypeService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<InstitutionTypeModel>>))]
        public async Task<IActionResult> Create([FromBody] InstitutionTypeCreateModel model)
        {
            var response = await _institutionTypeService.Create(model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<InstitutionTypeModel>>>))]
        public async Task<IActionResult> GetAll([FromQuery] InstitutionTypeFilterModel search, [FromQuery] int? pageSize, [FromQuery] int? page)
        {
            var response = await _institutionTypeService.GetAll(search, pageSize, page);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<InstitutionTypeModel>>))]
        public async Task<IActionResult> Get([FromRoute] long id)
        {
            var response = await _institutionTypeService.GetById(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<InstitutionTypeModel>>))]
        public async Task<IActionResult> Update([FromRoute] long id, [FromBody] InstitutionTypeCreateModel model)
        {
            var response = await _institutionTypeService.Update(id, model);
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
            var response = await _institutionTypeService.Delete(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
