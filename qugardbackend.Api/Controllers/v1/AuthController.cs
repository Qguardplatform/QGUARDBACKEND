using qguardbackend.Core.Interfaces;
using qguardbackend.Core.Services;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace qguardbackend.Controllers.v1
{
    [ApiVersion("1.0")]
    public class AuthController : BaseController
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;

        public AuthController(ILogger<AuthController> logger,
            IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        /// <summary>
        /// Endpoint to login to get token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {

            if (model == null)
            {
                return new StatusCodeResult(500);
            }

            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _authService.LoginAsync(model);

            if (result.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
 

        /// <summary>
        /// Endpoint to change password
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequestDto model)
        {

            if (model == null)
            {
                return new StatusCodeResult(500);
            }

            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _authService.ChangePasswordAsync(model.email, model.CurrentPassword, model.NewPassword);

            if (result.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        /// <summary>
        /// To reset user password with new password
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] PasswordResetRequestDto model)
        {

            if (model == null)
            {
                return new StatusCodeResult(500);
            }

            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _authService.ResetPasswordAsync(model.email, model.token, model.NewPassword);

            if (result.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequestDto request)
        {

            if (string.IsNullOrEmpty(request.email))
            {
                return new StatusCodeResult(500);
            }

            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _authService.ForgotPasswordAsync(request.email);

            if (result.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        [AllowAnonymous]
        [HttpPost("send-new-login-password")]
        public async Task<IActionResult> SendNewLogInPassword([FromBody] ForgotPasswordRequestDto request)
        {

            if (string.IsNullOrEmpty(request.email))
            {
                return new StatusCodeResult(500);
            }

            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _authService.SendNewLogInPassword(request.email);

            if (result.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }


        [AllowAnonymous]
        [HttpPost("send-new-login-password-bulk")]
        public async Task<IActionResult> SendNewLogInPasswordinBulk(DefaultPasswordEmailsListDto requests)
        {

            //if (string.IsNullOrEmpty(request.email))
            //{
            //    return new StatusCodeResult(500);
            //}

            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _authService.SendNewLogInPasswordBulk(requests);

            if (result.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }


        [AllowAnonymous]
        [HttpPost("validate-otp")]
        public async Task<IActionResult> ValidateOTP([FromBody] validateOTP request)
        {

            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _authService.ValidateOtp(request.UserId, request.otp);

            if (result.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }


        /// <summary>
        /// Regenerate and send Login OTP
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("generate-login-otp/{userId}")]
        public async Task<IActionResult> GenerateLoginOTP(string userId)
        {

            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _authService.ResendLoginAsync(userId);

            if (result.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }


        /// <summary>
        /// Endpoint to Refresh Token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto model)
        {
            if (model == null)
            {
                return new StatusCodeResult(500);
            }

            if (!ModelState.IsValid)
                return UnprocessableEntity(GetValidationErrors<bool>(ModelState));

            var result = await _authService.RefreshToken(model);

            if (result.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}