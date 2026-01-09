using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using Microsoft.AspNetCore.Mvc;
using examportal.Api.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class DepartmentController : BaseController
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<DepartmentDto>>))]
        public async Task<IActionResult> Create([FromBody] DepartmentCreateModel model)
        {
            var response = await _departmentService.Create(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }


        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<DepartmentDto>>>))]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] GetDeptQueryModelMini search)
        {
            var response = await _departmentService.GetAll(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<DepartmentDto>>))]
        [AllowAnonymous]
        public async Task<IActionResult> Get([FromRoute] long id)
        {
            var response = await _departmentService.GetById(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<DepartmentDto>>))]
        public async Task<IActionResult> Update([FromRoute] long id, [FromBody] DepartmentCreateModel model)
        {
            var response = await _departmentService.Update(id, model, CurrentUser.Email);
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
            var response = await _departmentService.Delete(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        [HttpPut("enable-and-disabled/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<string>>))]
        public async Task<IActionResult> ChangeStatus(long id)
        {
            var response = await _departmentService.EnableDisableLevel(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}