using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using Microsoft.AspNetCore.Http;

namespace qguardbackend.Core.Interfaces
{
    public interface IInstitutionService
    {
        Task<CustomResult<InstitutionResponseDto>> Create(InstitutionCreateModel model, string createdBy);
        Task<CustomResult<InstitutionResponseDto>> Update(long id, InstitutionCreateModel model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<string>> ChangeInstitutionStatus(long id, string createdBy);
        Task<CustomResult<InstitutionResponseDto>> GetById(long id);
        Task<CustomResult<PaginatedResult<InstitutionResponseDto>>> GetAll(InstitutionFilterModel search);
        Task<CustomResult<InstitutionResponseDto>> GetByCode(string code);
        Task SeedDefaultInstitution();
        Task<bool> CheckinstitutionByCode(string code);
        Task<CustomResult<InstitutionResponseDto>> AddInstitutionLogo(long id, IFormFile formData);
        Task<CustomResult<InstitutionResponseDto>> GetByHostName(string hostname);
    }
}