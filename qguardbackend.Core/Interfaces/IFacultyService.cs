using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;

namespace qguardbackend.Core.Interfaces
{
    public interface IFacultyService 
    {
        Task<CustomResult<FacultyDto>> Create(FalcultyCreateModel model, string createdBy);
        Task<CustomResult<List<MigrationErrorVM>>> Update(long id, FalcultyCreateModel model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<PaginatedResult<FacultyDto>>> GetAll(FacultyQueryModelMini search);
        Task<CustomResult<FacultyDto>> GetById(long id);
        Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy);
    }
}