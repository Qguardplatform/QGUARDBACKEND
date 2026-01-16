using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using qguardbackend.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace qguardbackend.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class RoleController : BaseController
    {
        private readonly IRoleService _roleRepository;

        public RoleController(IRoleService roleRepository)
        {
            _roleRepository = roleRepository;
        }

        [HttpGet]

        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<RoleModel[]>>))]
        public async Task<IActionResult> GetAll()
        {
            var response = await _roleRepository.GetRoles();
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<RoleModel>>))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _roleRepository.GetRoleById(id.ToString());
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("by-name/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<RoleModel>>))]
        public async Task<IActionResult> GetByName([FromRoute] string name)
        {
            var response = await _roleRepository.GetRoleByName(name);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
