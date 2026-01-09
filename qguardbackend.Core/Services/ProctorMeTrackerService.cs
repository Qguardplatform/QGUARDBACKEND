using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace qguardbackend.Core.Services
{
    public class ProctorMeTrackerService : IProctorMeTrackerService
    {
        private readonly AppDbContext _context;
        public ProctorMeTrackerService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<CustomResult<ProctorMeTrackerModel>> Create(ProctorMeTrackerModel model)
        {
            try
            {
                var record = JObject.FromObject(model).ToObject<ProctorMeTracker>();

                record.ActivityResponseDesc = model.ActivityResponseDesc;
                record.ActivityRequest = model.ActivityRequest;
                record.ActivityDescription = model.ActivityDescription;
                record.ActivityResponse = model.ActivityResponse;
                record.ApplicationState = model.ApplicationState;
                record.CreatedAt = DateTime.UtcNow;
                record.IsDeleted = false;
                record.IsActive = true;

                await _context.ProctorMeTrackers.AddAsync(record);
                await _context.SaveChangesAsync();

                return CustomResult<ProctorMeTrackerModel>.Success(model);
            }
            catch (Exception ex)
            {
                return CustomResult<ProctorMeTrackerModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<ProctorMeTrackerModel>>> GetAll(QueryModelMini search)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<ProctorMeTracker> records = _context.ProctorMeTrackers.OrderByDescending(x => x.CreatedAt);
                if (search.IsActive.HasValue)
                {
                    records = records.Where(x => x.IsActive == search.IsActive.Value);
                }
                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.ActivityDescription.ToLower().Contains(searchWord) ||
                        x.ActivityResponse.Contains(searchWord) ||
                        x.ActivityRequest.ToLower().Contains(searchWord) ||
                        x.ActivityResponseDesc.ToLower().Contains(searchWord) ||
                        x.ApplicationState.ToLower().Contains(searchWord)
                    );
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= search.StartDate && x.CreatedAt <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.Id);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }

                var result = await records.Select(query => new ProctorMeTrackerModel
                {
                    Id = query.Id,
                    ActivityRequest = query.ActivityRequest,
                    ApplicationState = query.ApplicationState,
                    ActivityResponseDesc = query.ActivityResponseDesc,
                    ActivityDescription = query.ActivityDescription,
                    ActivityResponse = query.ActivityResponse,
                    CreatedAt = query.CreatedAt
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<ProctorMeTrackerModel>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                return CustomResult<PaginatedResult<ProctorMeTrackerModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}