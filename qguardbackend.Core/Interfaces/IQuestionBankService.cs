using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.ResponseDto;

namespace qguardbackend.Core.Interfaces
{
    public interface IQuestionBankService
    {
        Task<CustomResult<string>> CreateQuestionAsync(QuestionBankCreateModel model);
        Task<CustomResult<PaginatedResult<QuestionBankModel>>>GetAllQuestionsAsync(QuestionBankQueryModelMini query);
        Task<CustomResult<PaginatedResult<ExamQuestionBankListDto>>>GetAllQuestionBanksAsync(QuestionBankQueryModelMini query);
        Task<CustomResult<QuestionBankModel>> GetQuestionByAsyncId(long id);
        Task<CustomResult<string>> UpdateQuestionAsync(long id, QuestionBankUpdateModel model);
        Task<CustomResult<string>> UpdateExamBankQuestionAsync( long examscheduleId, QuestionBankUpdateModel model);
        Task<CustomResult<string>> ToggleQuestionStatusAsync(long id);
        Task<CustomResult<string>> DeleteQuestionAsync(long id);
        byte[] GenerateSampleCsv(out string fileName, out string contentType);
        Task<CustomResult<List<MigrationErrorVM>>> UploadQuestionAsync(QuestionBankUploadModel model);
        Task<CustomResult<string>> DeleteMultipleQuestionsAsync(List<long> questionIds);
        Task<CustomResult<string>> AddQuestionOptionAsync(AddQuestionOptionModel model);
        Task<CustomResult<string>> UpdateQuestionOptionAsync(QuestionOptionUpdateModel model);
    }
}
