using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Error;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;
using examportal.Api.Controllers;
using examportal.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class AccountController : BaseController
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IUserManagementService _userManagementService;
        private readonly IAuthService _authService;

        public AccountController(ILogger<AccountController> logger, IAuthService authService,
           IUserManagementService userManagementService)
        {
            _logger = logger;
            _userManagementService = userManagementService;
            _authService = authService;
        }

        /// <summary>
        /// Endpoint to get the details of active users
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<PagedList<ApplicationUserResponse>>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Response<ModelErrorResponse>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(Response<string>))]
        [HttpGet("get-users")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUsersWithRolesAsync([FromQuery] UsersFilterModel model)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _userManagementService.GetUsersWithRolesAsync(model);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Endpoint to get the details of a user by the userId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<ApplicationUserResponse>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Response<ModelErrorResponse>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(Response<string>))]
        [HttpGet("get-user-by-id/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserDetailsByIdAsync(string userId)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _userManagementService.GetUserDetailsByIdAsync(userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Endpoint to Create other uses across other tenants
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //[AllowAnonymous]
        [HttpPost("create-other-users")]
        public async Task<IActionResult> RegisterOtherUsersAsync([FromBody] RegisterOtherUserRequestDto model)
        {
            if (model == null)
            {
                return new StatusCodeResult(500);
            }
            if (!ModelState.IsValid) return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _authService.RegisterOtherUsersAsync(model);
            if (result.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPut("update-user/{userid}")]
        public async Task<IActionResult> UpdateUserAsync(string userid, [FromForm] UpdateUserRequestDto model)
        {
            if (model == null) return new StatusCodeResult(500);

            if (!ModelState.IsValid) return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _authService.UpdateUserAsync(userid, model);
            if (result.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPut("change-status/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<bool>>))]
        public async Task<IActionResult> ChangeStatus(string userId)
        {
            var response = await _userManagementService.ChangeAccountStatus(userId);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Spool all users
        /// </summary>
        [HttpGet("export-users-data")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<ExportStudentExamsDto>>))]
        public async Task<IActionResult> ExportExam([FromQuery] UserExportModel search)
        {
            var response = await _userManagementService.ExportUsers(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}