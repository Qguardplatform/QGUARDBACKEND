using edutech.services.examportal.Core.Interfaces;
using edutech.services.examportal.Data.Common;
using edutech.services.examportal.Data.Constants;
using edutech.services.examportal.Data.DbContext;
using edutech.services.examportal.Data.DTOs;
using edutech.services.examportal.Data.DTOs.Results;
using edutech.services.examportal.Data.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace edutech.services.examportal.Core.Services
{
    public class InstitutionTypeService : IInstitutionTypeService
    {
        private readonly ILogger<InstitutionTypeService> _logger;
        private readonly AppDbContext _context;

        public InstitutionTypeService(ILogger<InstitutionTypeService> logger,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<CustomResult<InstitutionTypeModel>> Create(InstitutionTypeCreateModel model)
        {
            try
            {
                var check = await _context.Institutions.FirstOrDefaultAsync(x => x.Name.ToLower() == model.Name.ToLower());
                if (check is not null)
                {
                    return CustomResult<InstitutionTypeModel>.ErrorOccured("Institution type name already exist", ResponseCodes.AlreadyExistErrorCode);
                }
                var create = InstitutionType.Create(model.Name);
                await _context.AddAsync(create);
                await _context.SaveChangesAsync();
                var returnDto = await this.GetById(create.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<InstitutionTypeModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> Delete(long id)
        {
            try
            {
                var type = await _context.InstitutionTypes.FirstOrDefaultAsync(x => x.Id == id);
                if (type == null)
                {
                    return CustomResult<string>.ErrorOccured("Institution type not found!", ResponseCodes.NotFoundErrorCode);
                }
                var check = await _context.Institutions.Where(x => x.InstitutionTypeId == id).ToListAsync();
                if (check.Any())
                {
                    return CustomResult<string>.ErrorOccured("Institution type cannot be deleted because it has already been mapped with an Institution!", ResponseCodes.BadRequestErrorCode);
                }
                _context.Remove(type);
                await _context.SaveChangesAsync();
                return CustomResult<string>.Success("Institution type successfully deleted", ResponseCodes.SuccessCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<InstitutionTypeModel>>> GetAll(InstitutionTypeFilterModel search, int? pageSize, int? page)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Institution> records = _context.Institutions;

                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x => x.Name.ToLower().Contains(searchWord));
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= search.StartDate && x.CreatedAt <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.Name);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }
                var result = await records.Select(query => new InstitutionTypeModel
                {
                    Id = query.Id,
                    Name = query.Name,
                    DateCreated = query.CreatedAt
                }).ToListAsync();

                var paginatedResult = page.HasValue && pageSize.HasValue
                    ? result.ToPageList(page.Value, pageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<InstitutionTypeModel>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<InstitutionTypeModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<InstitutionTypeModel>> GetById(long id)
        {
            try
            {
                var query = await _context.Institutions.FirstOrDefaultAsync(c => c.Id == id);
                if (query == null)
                {
                    return CustomResult<InstitutionTypeModel>.ErrorOccured("Invalid Institution type!", ResponseCodes.BadRequestErrorCode);
                }
                var model = new InstitutionTypeModel
                {
                    Id = query.Id,
                    Name = query.Name,
                    DateCreated = query.CreatedAt
                };
                return CustomResult<InstitutionTypeModel>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<InstitutionTypeModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task SeedDefaultInstitutionType()
        {
            if (_context.InstitutionTypes.Any())
            {
                return;
            }
            List<InstitutionTypeCreateModel> myList = new List<InstitutionTypeCreateModel>
            {
                new InstitutionTypeCreateModel { Name = "Academic" },
                new InstitutionTypeCreateModel { Name = "Corporate" }
            };
            if (myList.Any())
            {
                foreach (var items in myList)
                {
                    var settings = InstitutionType.Create(items.Name);
                    await _context.AddAsync(settings);
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<CustomResult<InstitutionTypeModel>> Update(long id, InstitutionTypeCreateModel model)
        {
            try
            {
                var query = await _context.InstitutionTypes.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                {
                    return CustomResult<InstitutionTypeModel>.ErrorOccured("Institution type id does not exist!", ResponseCodes.BadRequestErrorCode);
                }

                query.Name = model.Name;
                query.UpdatedAt = DateTime.UtcNow;

                _context.Update(query);
                await _context.SaveChangesAsync();
                var returnDto = await this.GetById(query.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<InstitutionTypeModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}
