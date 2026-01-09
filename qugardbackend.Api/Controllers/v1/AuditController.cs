using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using examportal.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class AuditController : BaseController
    {
        private readonly IAuditLogService _auditLogSvc;

        public AuditController(IAuditLogService auditLogSvc)
        {
            _auditLogSvc = auditLogSvc;

        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<AuditLogReadModel>>>))]
        public async Task<IActionResult> Get([FromQuery] AuditLogFilterModel filter)
        {
            var response = await _auditLogSvc.GetAuditLog(filter);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<AuditLogReadModel>))]
        public async Task<IActionResult> GetSingleAuditLog([FromRoute] long id)
        {
            var response = await _auditLogSvc.GetAuditLogById(id);
            if(response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("user-activities/{email}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<AuditLogReadModel>>>))]
        public async Task<IActionResult> GetUserActivities([FromRoute]string email, [FromQuery] AuditLogFilterModel filter)
        {
            var response = await _auditLogSvc.GetUserAudit(email, filter);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("institution-activities/{code}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<AuditLogReadModel>>>))]
        public async Task<IActionResult> GetInstitutionActivities([FromRoute] string code, [FromQuery] AuditLogFilterModel filter)
        {
            var response = await _auditLogSvc.GetAuditByInstitutionCode(code, filter);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}