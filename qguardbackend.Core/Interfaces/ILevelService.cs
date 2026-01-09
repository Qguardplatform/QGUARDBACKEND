using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;

namespace qguardbackend.Core.Interfaces
{
    public interface ILevelService
    {
        Task<CustomResult<LevelModel>> Create(LevelCreateModel model, string createdBy);
        Task<CustomResult<LevelModel>> Update(long id, LevelCreateModel model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<PaginatedResult<LevelModel>>> GetAll(QueryModelMini search);
        Task<CustomResult<LevelModel>> GetById(long id);
        Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy);
    }
}