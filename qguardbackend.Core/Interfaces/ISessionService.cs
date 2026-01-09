using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;

namespace qguardbackend.Core.Interfaces
{
    public interface ISessionService
    {
        Task<CustomResult<SessionDto>> Create(SessionCreateModel model, string createdBy);
        Task<CustomResult<SessionDto>> Update(long id, SessionCreateModel model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<PaginatedResult<SessionDto>>> GetAll(QueryModelMini search);
        Task<CustomResult<SessionDto>> GetById(long id);
        Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy);
        Task SeedDefaultSession(); 
    }
}
