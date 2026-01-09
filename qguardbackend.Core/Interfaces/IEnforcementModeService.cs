using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;

namespace qguardbackend.Core.Interfaces
{
    public interface IEnforcementModeService
    {
        Task<CustomResult<EnforcementModeModel>> Create(EnforcementModeCreateModel model, string createdBy);
        Task<CustomResult<EnforcementModeModel>> Update(long id, EnforcementModeCreateModel model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<PaginatedResult<EnforcementModeModel>>> GetAll(QueryModelMini search);
        Task<CustomResult<EnforcementModeModel>> GetById(long id);
        Task SeedDefaultEnforcementMode();
    }
}