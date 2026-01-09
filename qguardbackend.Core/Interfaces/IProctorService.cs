using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Enums;

namespace qguardbackend.Core.Interfaces
{
    public interface IProctorService
    {
        Task<CustomResult<string>> ProcessProctorFlagAsync(ProctorWebhookPayload payload);
        Task<CustomResult<string>> CreateSessionAsync(ExamSessionRequest model);
        Task<CustomResult<ProctoringDashboardResponse>> GetProctoringDashboardAsync(QueryModelMini query);
        Task<CustomResult<ProctoringExamDetailResponse>> GetProctoringExamDetailsAsync(long examScheduleId, QueryModelMini query);
        Task<CustomResult<CandidateProctorDetailsResponse>> GetCandidateProctorDetailsAsync(long candidateId, long examScheduleId);
        Task<CustomResult<string>> ProcessProctorActionAsync(long candidateId, long examScheduleId, ProctorActionType action);
    }
}