using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.Enums;

namespace qguardbackend.Core.Interfaces
{
    public interface IExamScheduleService
    {
        Task<CustomResult<ExamScheduleDto>> CreateAsync(ExamScheduleCreateDto dto, string createdBy);
        Task<CustomResult<string>> UpdateAsync(long id, ExamScheduleCreateDto dto, string createdBy);
        Task<CustomResult<string>> DeleteAsync(long id, string createdBy);
        Task<CustomResult<ExamScheduleDto>> GetByIdAsync(long id);
        Task<CustomResult<PaginatedResult<ExamScheduleDto>>> GetAllAsync(ExamSchedulenFilterModel query);
        Task<CustomResult<PaginatedResult<ExamScheduleDto>>> GetAllExamScheduledByTimingAsync
            (ExamSchedulenDashboardFilterModel query, string candidateuserId);
        Task<CustomResult<string>> ChangeExamScheduleStatus(long id, ExamScheduleStatusEnum status, string createdBy);
    }
}