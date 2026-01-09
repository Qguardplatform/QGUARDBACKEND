using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.RequestDto;

namespace qguardbackend.Core.Interfaces
{
    public interface ICandidateService
    {
        Task<CustomResult<CreateUpdateCandidateResponseDto>> Create(CandidateRequestDto model);
        Task<CustomResult<CreateUpdateCandidateResponseDto>> Update(long id, UpdateCandidateRequestDto model);
        Task<CustomResult<UpdateCandidateStatuResponseDto>> UpdateCandidateState(UpdateCandidateStatusRequestDto model);
        Task<CustomResult<CandidateModel>> GetById(long id);
        Task<CustomResult<CandidateModel>> GetCandidateDetailByUserId(string candidateuserid);
        Task<CustomResult<bool>> Delete(long id, string createdBy);
        Task<CustomResult<string>> EnableDisableCandidate (long id, string createdBy);
        Task<CustomResult<List<List<string>>>> GetDownloadFormat();
        Task<CustomResult<List<MigrationErrorVM>>> BulkUploadCandidatesAsync(CandidateUploadModel uploadModel);
        Task<CustomResult<PaginatedResult<CandidateModel>>> GetAllCandidates(CandidateFilterModel search);
        Task<CustomResult<PaginatedResult<CandidateModel>>> GetCandidatesByInstitutionId(long id, CandidateFilterModel search);
        Task<CustomResult<PaginatedResult<CandidateModel>>> GetCandidatesByInstitutionCode(string code, CandidateFilterModel search);
        Task<CustomResult<CandidatePreviewModel>> CandidateProfilePreview(string email);
        Task<CustomResult<ExportStudentExamsDto>> ExportCandidates(CandidateExportModel search);
    }
}