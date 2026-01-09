using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;

namespace qguardbackend.Core.Interfaces
{
    public interface IAuditLogService
    {
        Task<CustomResult<PaginatedResult<AuditLogReadModel>>> GetAuditLog(AuditLogFilterModel filter);
        Task<bool> LogToAudit(AuditLogWriteModel model);
        Task AddToAudit(int EventType, string ActionClass, string description);
        Task<CustomResult<AuditLogReadModel>> GetAuditLogById(long id);
        Task<CustomResult<string>> Delete(long id);
        Task<CustomResult<PaginatedResult<AuditLogReadModel>>> GetUserAudit(string email, AuditLogFilterModel filter);
        Task<CustomResult<PaginatedResult<AuditLogReadModel>>> GetAuditByInstitutionCode(string code, AuditLogFilterModel filter);
    }
}