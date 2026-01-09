using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;

namespace qguardbackend.Core.Interfaces
{
    public interface IProgramService
    {
        Task<CustomResult<ProgramModel>> Create(ProgramCreateModel model, string createdBy);
        Task<CustomResult<string>> Update(long id, ProgramCreateModel model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<PaginatedResult<ProgramModel>>> GetAll(GetProgramQueryModelMini search);
        Task<CustomResult<ProgramModel>> GetById(long id);
        Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy);
    }
}