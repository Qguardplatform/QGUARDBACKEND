using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;

namespace qguardbackend.Core.Interfaces
{
    public interface ISemesterService
    {
        Task<CustomResult<SemesterModel>> Create(SemesterCreateModel model, string createdBy);
        Task<CustomResult<SemesterModel>> Update(long id, SemesterCreateModel model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<PaginatedResult<SemesterModel>>> GetAll(QueryModelMini search);
        Task<CustomResult<SemesterModel>> GetById(long id);
        Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy);
        Task SeedDefaultSemester();
    }
}