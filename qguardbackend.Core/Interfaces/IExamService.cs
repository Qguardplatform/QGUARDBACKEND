using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;

namespace qguardbackend.Core.Interfaces
{
    public interface IExamService
    {
        Task<CustomResult<string>> CreateQuestionForExamAsync(ExamQuestionCreateModel model, string createdBy);
        Task<CustomResult<string>> MapQuestionsToExamSchedule(MapQuestionsToExamScheduleRequestDto model, string createdBy);
        Task<CustomResult<QuestionBankModel>> CreateSingleQuestionForExamAsync(SingleExamQuestionCreateModel model, string createdBy);
        Task<CustomResult<ExamSubmissionAnswersResponseDto>> SubmitExaminationAnswersAsync(ExamSubmissionRequestDto model, string createdBy);
        Task<CustomResult<ExamQuestionCreateDto>> UpdateAsync(long id, ExamQuestionCreateDto dto, string createdBy);
        Task<CustomResult<string>> DeleteAsync(long id, string createdBy);
        Task<CustomResult<ExamQuestionListModel>> GetByIdAsync(long id);
        Task<CustomResult<PaginatedResult<ExamQuestionListModel>>> GetAllAsync(ExamQuestionFilterModel filter);
        Task<List<long>> CreateQuestionAsync(long courseId, ExamQuestionMultModel model);
        Task<CustomResult<PaginatedResult<CandidateExamScheduleModel>>> CandidatePulishedExamSchedule(string candidateId, QueryModelMini query);
        Task<CustomResult<ExamInstructionModel>> GetExamInstruction(long examScheduleId);
        Task<CustomResult<ExamCreateModel>> QuestionsInExamSchedule(long examScheduleId, int? PageNumber, int? PageSize);
        Task<CustomResult<ExamCreateModelForCandidate>> QuestionsInExamScheduleForACandidate(string candidateuserid, long examScheduleId, int? PageNumber, int? PageSize);
        Task<CustomResult<ExamCreateModelNoCorrectoption>> GetQuestionsAndOptionsNoCorrectOption(long examScheduleId, int? PageNumber, int? PageSize);
        Task<CustomResult<viewCountUpdateResponse>> UpdateCandidateViewCount(long examScheduleId, long candidateId);
        Task<CustomResult<PaginatedResult<CandidateExamListModel>>> CandidateResultList(string candidateId, ExamQuestionFilterModel query);
        Task<CustomResult<ExamSubmissionDetailModel>> GetCandidateExaminationDetails(long examScheduleId, long candidateId, int? PageNumber, int? PageSize);
        Task<CustomResult<PaginatedResult<ScheduleExamQuestionListModel>>> AllResultSubmissionExam(ScheduleExamQuestionFilterModel query);
        Task<CustomResult<PaginatedResult<DeptAndFacultyScheduleExamQuestionListModel>>> AllDepartmentResultSubmissionExam(DepartmeentFacultyScheduleExamQuestionFilterModel query);
        Task<CustomResult<SingleExamResultListModel>> SingleExamRecordsByCourse(long courseId, ExamRecordsByDepartmentFilterModel search);
        Task<CustomResult<string>> PublishResult(long examScheduleId, string createdBy);
        Task<CustomResult<string>> AllowCandidateExamResit(long examScheduleId, long candidateId, string createdBy);
        Task<CustomResult<string>> AllowMultipleCandidatesExamResit(AllowMultipleCandidatesExamResitRequestDto request, string updatedBy);
        Task<CustomResult<QuestionBankModel>> RemoveMappedSingleQuestionFromExamAsync(long examScheduleId, long questionBankId, string createdBy);
        Task<CustomResult<PaginatedResult<StudentExamsDto>>> CandidateExamsResultList(long candidateId, StudentExamDoneFilterModel search);
        Task<CustomResult<StudentExamDetailsDto>> CandidateExamsResultDetailsByExamSchedule(long candidateId, long examScheduleId);
        Task<CustomResult<ExportStudentExamsDto>> ExportCandidateExams(long examScheduleId);
    }
}