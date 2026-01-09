using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;

namespace qguardbackend.Core.Interfaces
{
    public interface IDepartmentService
    {
        Task<CustomResult<DepartmentDto>> Create(DepartmentCreateModel model, string createdBy);
        Task<CustomResult<string>> Update(long id, DepartmentCreateModel model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<PaginatedResult<DepartmentDto>>> GetAll(GetDeptQueryModelMini search);
        Task<CustomResult<DepartmentDto>> GetById(long id);
        Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy);
    }
}
