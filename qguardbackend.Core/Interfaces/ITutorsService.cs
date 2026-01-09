using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.ResponseDto;

namespace qguardbackend.Core.Interfaces
{
    public interface ITutorsService
    {      
        Task<CustomResult<PaginatedResult<InstitutionTutorsResponseDto>>> GetAll(QueryModelMini search);
        Task<CustomResult<InstitutionTutorsResponseDto>> GetById(long id);
        Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy);
    }
}