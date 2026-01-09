using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Security.Claims;

namespace qguardbackend.Core.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuditLogService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserManagementService _userManagementService;

        public AuditLogService(AppDbContext context,
            ILogger<AuditLogService> logger,
            IUserManagementService userManagementService,
        IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _userManagementService = userManagementService;
        }

        private DateTime AddTimeSpan(DateTime date)
        {
            var tm = TimeSpan.Parse("23:59:59");
            DateTime endDate = date + tm;
            return endDate;
        }

        public async Task<CustomResult<PaginatedResult<AuditLogReadModel>>> GetAuditLog(AuditLogFilterModel filter)
        {
            try
            {
                IQueryable<AuditLog> records = _context.AuditLogs
                                            .Include(x => x.Institution)
                                            .OrderByDescending(x => x.CreatedAt);

                var end = AddTimeSpan(filter.EndDate ?? DateTime.Now);

                if (!string.IsNullOrEmpty(filter.CreatedBy))
                {
                    records = records.Where(x => x.UserId.ToLower() == filter.CreatedBy.ToLower());
                }
               
                if (!string.IsNullOrEmpty(filter.Action))
                {
                    records = records.Where(x => x.Action.ToLower() == filter.Action.ToLower());
                }
                if (!string.IsNullOrEmpty(filter.EventType))
                {
                    if (Enum.TryParse(filter.EventType, true, out AuditActionType actionType))
                    {
                        records = records.Where(x => x.EventType == (int)actionType);
                    }
                }

                if (!string.IsNullOrWhiteSpace(filter.SearchWord))
                {
                    var searchWord = filter.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.UserId.ToLower().Contains(searchWord) ||
                        x.Institution.Name.ToLower().Contains(searchWord) ||
                        x.Description.ToLower().Contains(searchWord) ||
                        x.Action.ToLower().Contains(searchWord) ||
                        x.IPAddress.ToLower().Contains(searchWord));
                }

                if (filter.StartDate.HasValue && filter.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= filter.StartDate.Value && x.CreatedAt <= end);
                }
                var tenCode = await _userManagementService.GetTenantCode();
                if (!string.IsNullOrEmpty(tenCode.Data)
                    && tenCode.Data.ToLower() !="master"
                    )
                {
                    records = records.Where(x => x.Institution.Code.ToLower()==tenCode.Data.ToLower());

                }

                if (filter.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.CreatedAt);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }

                var resultData = await records.Select(record => new AuditLogReadModel
                {
                    Id = record.Id,
                    EventType = ((AuditActionType)record.EventType).ToString(),
                    IPAddress = record.IPAddress,
                    Action = record.Action,
                    UserName = string.IsNullOrEmpty(record.UserId) ? "n/a" : record.UserId,
                    Description = string.IsNullOrEmpty(record.Description) ? "n/a" : record.Description,
                    Institution = record.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = record.Institution.Id,
                        Name = record.Institution.Name,
                        Code = record.Institution.Code
                    },
                    CreatedDate = record.CreatedAt
                }).ToListAsync();

                // Handle pagination
                var responseData = filter.PageNumber.HasValue && filter.PageSize.HasValue
                    ? resultData.ToPageList(filter.PageNumber.Value, filter.PageSize.Value)
                    : resultData.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<AuditLogReadModel>>.Success(responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        public async Task<CustomResult<PaginatedResult<AuditLogReadModel>>> GetUserAudit(string email, AuditLogFilterModel filter)
        {
            try
            {
                var tenantIdResult = await _userManagementService.GetTenantId();
                if (!tenantIdResult.IsSuccess)
                    return CustomResult<PaginatedResult<AuditLogReadModel>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                IQueryable<AuditLog> records = _context.AuditLogs
                                            .Include(x => x.Institution)
                                            .Where(x => x.UserId.ToLower() == email.ToLower() && x.InstitutionId == tenantIdResult.Data);

                var end = AddTimeSpan(filter.EndDate ?? DateTime.Now);

                if (!string.IsNullOrEmpty(filter.CreatedBy))
                {
                    records = records.Where(x => x.UserId.ToLower() == filter.CreatedBy.ToLower());
                }
                var tenCode = await _userManagementService.GetTenantCode();
                if (!string.IsNullOrEmpty(tenCode.Data)
                    && tenCode.Data.ToLower() == "master"
                    )
                {
                    records = records.Where(x => x.Institution.Code.ToLower() == tenCode.Data.ToLower());

                }
                if (!string.IsNullOrEmpty(filter.Action))
                {
                    records = records.Where(x => x.Action.ToLower() == filter.Action.ToLower());
                }
                if (!string.IsNullOrEmpty(filter.EventType))
                {
                    if (Enum.TryParse(filter.EventType, true, out AuditActionType actionType))
                    {
                        records = records.Where(x => x.EventType == (int)actionType);
                    }
                }
                if (!string.IsNullOrWhiteSpace(filter.SearchWord))
                {
                    var searchWord = filter.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.UserId.ToLower().Contains(searchWord) ||
                        x.Institution.Name.ToLower().Contains(searchWord) ||
                        x.Description.ToLower().Contains(searchWord) ||
                        x.Action.ToLower().Contains(searchWord) ||
                        x.IPAddress.ToLower().Contains(searchWord));
                }

                if (filter.StartDate.HasValue && filter.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= filter.StartDate.Value && x.CreatedAt <= end);
                }

                if (filter.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.CreatedAt);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }

                var resultData = await records.Select(record => new AuditLogReadModel
                {
                    Id = record.Id,
                    EventType = ((AuditActionType)record.EventType).ToString(),
                    IPAddress = record.IPAddress,
                    Action = record.Action,
                    UserName = string.IsNullOrEmpty(record.UserId) ? "n/a" : record.UserId,
                    Description = string.IsNullOrEmpty(record.Description) ? "n/a" : record.Description,
                    Institution = record.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = record.Institution.Id,
                        Name = record.Institution.Name,
                        Code = record.Institution.Code
                    },
                    CreatedDate = record.CreatedAt
                }).ToListAsync();

                // Handle pagination
                var responseData = filter.PageNumber.HasValue && filter.PageSize.HasValue
                    ? resultData.ToPageList(filter.PageNumber.Value, filter.PageSize.Value)
                    : resultData.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<AuditLogReadModel>>.Success(responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        public async Task<CustomResult<PaginatedResult<AuditLogReadModel>>> GetAuditByInstitutionCode(string code, AuditLogFilterModel filter)
        {
            try
            {
                IQueryable<AuditLog> records = _context.AuditLogs
                                            .Include(x => x.Institution)
                                            .Where(x => x.Institution.Code.ToLower() == code.ToLower());

                var end = AddTimeSpan(filter.EndDate ?? DateTime.Now);

                if (!string.IsNullOrEmpty(filter.CreatedBy))
                {
                    records = records.Where(x => x.UserId.ToLower() == filter.CreatedBy.ToLower());
                }
              
                if (!string.IsNullOrEmpty(filter.Action))
                {
                    records = records.Where(x => x.Action.ToLower() == filter.Action.ToLower());
                }
                if (!string.IsNullOrEmpty(filter.EventType))
                {
                    if (Enum.TryParse(filter.EventType, true, out AuditActionType actionType))
                    {
                        records = records.Where(x => x.EventType == (int)actionType);
                    }
                }
                if (!string.IsNullOrWhiteSpace(filter.SearchWord))
                {
                    var searchWord = filter.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.UserId.ToLower().Contains(searchWord) ||
                        x.Institution.Name.ToLower().Contains(searchWord) ||
                        x.Description.ToLower().Contains(searchWord) ||
                        x.Action.ToLower().Contains(searchWord) ||
                        x.IPAddress.ToLower().Contains(searchWord));
                }

                if (filter.StartDate.HasValue && filter.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= filter.StartDate.Value && x.CreatedAt <= end);
                }

                if (filter.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.CreatedAt);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }

                var resultData = await records.Select(record => new AuditLogReadModel
                {
                    Id = record.Id,
                    EventType = ((AuditActionType)record.EventType).ToString(),
                    IPAddress = record.IPAddress,
                    Action = record.Action + " | " + ((AuditActionType)record.EventType).ToString(),
                    UserName = string.IsNullOrEmpty(record.UserId) ? "n/a" : record.UserId,
                    Description = string.IsNullOrEmpty(record.Description) ? "n/a" : record.Description,
                    Institution = record.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = record.Institution.Id,
                        Name = record.Institution.Name,
                        Code = record.Institution.Code
                    },
                    CreatedDate = record.CreatedAt
                }).ToListAsync();

                // Handle pagination
                var responseData = filter.PageNumber.HasValue && filter.PageSize.HasValue
                    ? resultData.ToPageList(filter.PageNumber.Value, filter.PageSize.Value)
                    : resultData.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<AuditLogReadModel>>.Success(responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        public async Task<CustomResult<AuditLogReadModel>> GetAuditLogById(long id)
        {
            try
            {
                var record = await _context.AuditLogs.Include(x => x.Institution).FirstOrDefaultAsync(x => x.Id == id);
                if (record == null)
                {
                    return CustomResult<AuditLogReadModel>.ErrorOccured("Record not found", ResponseCodes.NotFoundErrorCode);
                }

                var model = new AuditLogReadModel
                {
                    Id = record.Id,
                    EventType = ((AuditActionType)record.EventType).ToString(),
                    IPAddress = record.IPAddress,
                    Action = record.Action + " " + ((AuditActionType)record.EventType).ToString(),
                    UserName = string.IsNullOrEmpty(record.UserId) ? "n/a" : record.UserId,
                    Description = string.IsNullOrEmpty(record.Description) ? "n/a" : record.Description,
                    Institution = record.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = record.Institution.Id,
                        Name = record.Institution.Name,
                        Code = record.Institution.Code
                    },
                    CreatedDate = record.CreatedAt
                };
                return CustomResult<AuditLogReadModel>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<AuditLogReadModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<bool> LogToAudit(AuditLogWriteModel model)
        {
            try
            {
                var newAudit = JObject.FromObject(model).ToObject<AuditLog>();
                await _context.AuditLogs.AddAsync(newAudit);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        public async Task AddToAudit(int EventType, string ActionClass, string description)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                string IPAddress = string.Empty;
                var eventType = Convert.ToInt32(EventType);
                string UserName = !_httpContextAccessor.HttpContext.User.Claims.Any() ? String.Empty : _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimValueTypes.Email).Value;
                try
                {
                    IPHostEntry heserver = Dns.GetHostEntry(Dns.GetHostName());
                    IPAddress = heserver.AddressList.ToList().Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault().ToString();
                }
                catch
                {
                    IPAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                }
                var activity = AuditLog.Create(eventType, UserName, IPAddress, ActionClass, description, InsTId.Data);
                await _context.AuditLogs.AddAsync(activity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        public async Task<CustomResult<string>> Delete(long id)
        {
            try
            {
                var audit = await _context.AuditLogs.FirstOrDefaultAsync(x => x.Id == id);
                if (audit is null)
                {
                    return CustomResult<string>.ErrorOccured("Record not found", ResponseCodes.NotFoundErrorCode);
                }

                _context.AuditLogs.Remove(audit);
                await _context.SaveChangesAsync();

                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Audit successfully deleted");
            }
            catch (Exception ex)
            {
                return CustomResult<string>.ErrorOccured($"{ex.Message}", ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}
