using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;

namespace qguardbackend.Core.Interfaces
{
    public interface ICourseService
    {
        Task<CustomResult<CourseResponseDto>> Create(CourseRequestDto model, string createdBy);
        Task<CustomResult<CourseResponseDto>> Update(long id, CourseRequestDto model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<PaginatedResult<CourseResponseDto>>> GetAll(CourseQueryModelMini search);
        Task<CustomResult<CourseResponseDto>> GetById(long id);
        Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy);
    }
}
