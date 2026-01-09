using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;

namespace qguardbackend.Core.Interfaces
{
    public interface IEnforcementActionService
    {
        Task<CustomResult<EnforcementActionModel>> Create(EnforcementActionCreateModel model, string createdBy);
        Task<CustomResult<EnforcementActionModel>> Update(long id, EnforcementActionCreateModel model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<PaginatedResult<EnforcementActionModel>>> GetAll(QueryModelMini search);
        Task<CustomResult<EnforcementActionModel>> GetById(long id);
        Task SeedDefaultEnforcementAction();
    }
}