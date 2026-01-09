using edutech.services.examportal.Data.DTOs.Results;
using edutech.services.examportal.Data.DTOs;
using edutech.services.examportal.Core.Autofac;

namespace edutech.services.examportal.Core.Interfaces
{
    public interface IInstitutionTypeService : IAutoDependencyCore
    {
        Task<CustomResult<InstitutionTypeModel>> Create(InstitutionTypeCreateModel model);
        Task<CustomResult<InstitutionTypeModel>> Update(long id, InstitutionTypeCreateModel model);
        Task<CustomResult<string>> Delete(long id);
        Task<CustomResult<PaginatedResult<InstitutionTypeModel>>> GetAll(InstitutionTypeFilterModel search, int? pageSize, int? page);
        Task<CustomResult<InstitutionTypeModel>> GetById(long id);
        Task SeedDefaultInstitutionType();
    }
}