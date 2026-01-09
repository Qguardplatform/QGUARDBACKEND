using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Enums;

namespace qguardbackend.Data.DTOs.RequestDto
{
    public class ProctorWebhookPayload
    {
        public string FlagId { get; set; }
        public string CandidateId { get; set; }
        public string ExamId { get; set; }
        public string Type { get; set; }
        public string Domain { get; set; }
        public string MediaUrl { get; set; }
        public string Description { get; set; }
        public string Event { get; set; } 
    }

    public class ExamSessionRequest
    {
        public long CandidateId { get; set; }
        public long ExamId { get; set; }
    }

    public class ProctoringSummaryDto
    {
        public int TotalFlags { get; set; }
        public int PendingReview { get; set; }
        public int ReviewEvents { get; set; } 
        public int TotalCandidates { get; set; }
    }

    public class ProctoringExamListDto
    {
        public long ExamScheduleId { get; set; }
        public string ExamTitle { get; set; }
        public int CandidateCount { get; set; }
        public int TotalFlags { get; set; }
        public string Status { get; set; }
        public DateTime ExamDate { get; set; }
    }

    public class ProctoringDashboardResponse
    {
        public ProctoringSummaryDto Summary { get; set; }
        public PaginatedResult<ProctoringExamListDto> ExamList { get; set; }
    }

    public class CandidateProctoringDetailDto
    {
        public long CandidateId { get; set; }
        public long ExamScheduleId { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public int TotalFlags { get; set; }
        public int PendingFlags { get; set; }
        public int ReviewedFlags { get; set; }
        public string Status { get; set; } 
    }

    public class ProctoringExamDetailResponse
    {
        public ProctoringSummaryDto Summary { get; set; }
        public PaginatedResult<CandidateProctoringDetailDto> Candidates { get; set; }
    }

    public class CandidateProctorSummaryDto
    {
        public int TotalFlags { get; set; }
        public int PendingReview { get; set; }
        public int Penalised { get; set; }
        public int Dismissed { get; set; }
    }

    public class CandidateViolationDto
    {
        public long LogId { get; set; }
        public string FlagType { get; set; } // e.g. "FACE ABSENCE"
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; }
        public string Penalty { get; set; }  // pulled from proctor config
        public string Status { get; set; }   // Pending, Penalised, Dismissed
    }

    public class CandidateProctorDetailsResponse
    {
        public string CandidateName { get; set; }
        public CandidateProctorSummaryDto Summary { get; set; }
        public List<CandidateViolationDto> Violations { get; set; }
    }
    public class ProctorActionRequest
    {
        public long CandidateId { get; set; }
        public long ExamScheduleId { get; set; }
        public ProctorActionType ActionType { get; set; }
    }
}