using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace qguardbackend.Controllers.v1
{
    [ApiVersion("1.0")]
    public class SettingsController : BaseController
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CreateOtherSettingModel>))]
        public async Task<IActionResult> CreateSettings([FromBody] CreateOtherSettingModel model)
        {
            if (!ModelState.IsValid) return BadRequest(GetModelStateErrors(ModelState));
            var response = await _settingsService.CreateSettings(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<IEnumerable<OtherSettingsModel>>))]
        public async Task<IActionResult> GetAllSettings()
        {
            var response = await _settingsService.GetAllSettings();
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("update")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<List<OtherSettingModel>>))]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingModel model)
        {
            var response = await _settingsService.UpdateSettings(model.Name, model.Settings, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<string>))]
        public async Task<IActionResult> DeleteSettings([FromRoute]long id)
        {
            var response = await _settingsService.DeleteSettings(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}