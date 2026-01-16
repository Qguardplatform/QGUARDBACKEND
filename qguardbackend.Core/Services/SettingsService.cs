using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace qguardbackend.Core.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private readonly AppDbContext _context;
        //private readonly IAuditLogService _auditLogService;

        public SettingsService(ILogger<SettingsService> logger,
            AppDbContext context
            //,
            //IAuditLogService auditLogService
            )
        {
            _logger = logger;
            _context = context;
            //_auditLogService = auditLogService;
        }
        public async Task<CustomResult<CreateOtherSettingModel>> CreateSettings(CreateOtherSettingModel model, string createdBy)
        {
            try
            {
                if (model.Settings.Any())
                {
                    foreach (var item in model.Settings)
                    {
                        var check = _context.Settings.Where(x => x.Key == item.Parameter && x.Value == item.Value && x.Name.ToLower() == model.Name.ToLower()).FirstOrDefault();
                        if (check != null)
                        {
                            return CustomResult<CreateOtherSettingModel>.ErrorOccured($"{item.Parameter} => {item.Value} already exist for this {model.Name}!", ResponseCodes.BadRequestErrorCode);
                        }
                        var settings = Setting.Create(item.Parameter, item.Value, model.Name);
                        await _context.Settings.AddAsync(settings);
                    }
                    await _context.SaveChangesAsync();
                    //await _auditLogService.AddToAudit((int)AuditActionType.Create, "Settings", $"User [{createdBy}] created a new settings - {model.Name} at {DateTime.UtcNow}.");
                    return CustomResult<CreateOtherSettingModel>.Success(model);
                }
                return CustomResult<CreateOtherSettingModel>.ErrorOccured("Invalid request", ResponseCodes.BadRequestErrorCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<CreateOtherSettingModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> DeleteSettings(long id, string createdBy)
        {
            try
            {
                var setting = await _context.Settings.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (setting == null)
                {
                    return CustomResult<string>.ErrorOccured($"Record not found", ResponseCodes.NotFoundErrorCode);
                }
                _context.Settings.Remove(setting);
                await _context.SaveChangesAsync();
                //await _auditLogService.AddToAudit((int)AuditActionType.Delete, "Settings", $"User [{createdBy}] deleted a setting - {setting.Name} at {DateTime.UtcNow}.");

                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Settings successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public Task<CustomResult<IEnumerable<OtherSettingsModel>>> GetAllSettings()
        {
            try
            {
                var settings = _context.Settings.ToList().GroupBy(g => g.Name)
                            .Select(p => new OtherSettingsModel
                            {
                                Name = p.Key,
                                settings = p.Select(x => new OtherSettingListModel { Id = x.Id.ToString(), Parameter = x.Key, Value = x.Value }).ToList()
                            }).AsEnumerable();

                return Task.FromResult(CustomResult<IEnumerable<OtherSettingsModel>>.Success(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult(CustomResult<IEnumerable<OtherSettingsModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode));
            }
        }

        public async Task<CustomResult<string>> GetConfigurationSettings(string name, string condition)
        {
            try
            {
                List<Setting> configuration = await _context.Settings.Where(o => o.Name.ToLower() == name.ToLower()).ToListAsync();
                if (configuration == null)
                {
                    return CustomResult<string>.ErrorOccured($"Setting is empty!", ResponseCodes.NotFoundErrorCode);
                }
                var settings = configuration.Where(p => p.Key == condition).Select(p => p.Value).FirstOrDefault();
                if (settings == null)
                {
                    return CustomResult<string>.ErrorOccured($"{condition} has not been configured, contact the administrator!", ResponseCodes.NotFoundErrorCode);
                }
                return CustomResult<string>.Success(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<Dictionary<string, string>> GetConfigurationSettings(string name)
        {
            try
            {
                Dictionary<string, string> settings = new Dictionary<string, string> { };

                List<Setting> configuration = await _context.Settings.Where(o => o.Name.ToLower() == name.ToLower()).ToListAsync();
                if (configuration == null) throw new Exception("Setting is empty!");
                foreach (var item in configuration)
                {
                    settings.Add(item.Key, item.Value);
                }
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception($"{ex.Message}");
            }
        }

        public Task<CustomResult<IEnumerable<OtherSettingsModel>>> GetSettingsByName(string name)
        {
            try
            {
                var settings = _context.Settings.Where(x => x.Name.ToLower() == name.ToLower())
                           .ToList().GroupBy(g => g.Name)
                           .Select(p => new OtherSettingsModel
                           {
                               Name = p.Key,
                               settings = p.Select(x => new OtherSettingListModel { Id = x.Id.ToString(), Parameter = x.Key, Value = x.Value }).ToList()
                           }).AsEnumerable();

                return Task.FromResult(CustomResult<IEnumerable<OtherSettingsModel>>.Success(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult(CustomResult<IEnumerable<OtherSettingsModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode));
            }
        }

        public async Task SeedSettings()
        {
            if (_context.Settings.Any())
            {
                return;
            }
            else
            {
                List<CreateOtherSettingModel> myList = new List<CreateOtherSettingModel>
                {
                    new CreateOtherSettingModel {
                        Name = "ProctorMe",
                        Settings = new List<OtherSettingModel>
                        {
                            new OtherSettingModel
                            {
                                Parameter = "ApiKey",
                                Value = "key_f7fc56b8-fd27-4b10-9bcb-a7e6110a6122:54fe811e79f445a79f2ca6026b330505",
                            },
                            new OtherSettingModel
                            {
                                Parameter = "BaseURL",
                                Value = "https://api.proctorme.com",
                            }
                        }
                    },
                    new CreateOtherSettingModel {
                        Name = "AmazonS3",
                        Settings = new List<OtherSettingModel>
                        {
                            new OtherSettingModel
                            {
                                Parameter = "Region",
                                Value = "us-east-1",
                            },
                            new OtherSettingModel
                            {
                                Parameter = "SecretKey",
                                Value = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
                            },
                            new OtherSettingModel
                            {
                                Parameter = "AccessKey",
                                Value = "xxxxxxxxxxxxxxx",
                            },
                            new OtherSettingModel
                            {
                                Parameter = "BucketName",
                                Value = "sis-test-storage",
                            }
                        }
                    },
                };
                if (myList.Any())
                {
                    foreach (var items in myList)
                    {
                        foreach (var item in items.Settings)
                        {
                            var settings = Setting.Create(item.Parameter, item.Value, items.Name);
                            await _context.AddAsync(settings);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<CustomResult<List<OtherSettingModel>>> UpdateSettings(string name, List<OtherSettingModel> model, string createdBy)
        {
            try
            {
                var settings = _context.Settings.Where(x => x.Name.ToLower() == name.ToLower()).ToList();
                if (settings.Any())
                {
                    // override the previous setting with the new settings
                    foreach (var item in settings)
                    {
                        _context.Settings.Remove(item);
                    }
                    if (model.Any())
                    {
                        foreach (var items in model)
                        {
                            var setting = Setting.Create(items.Parameter, items.Value, name);
                            await _context.Settings.AddAsync(setting);
                        }
                    }
                    await _context.SaveChangesAsync();
                    //await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Settings", $"User [{createdBy}] update a setting - {name} at {DateTime.UtcNow}.");
                    return CustomResult<List<OtherSettingModel>>.Success(model);
                }
                else
                {
                    return CustomResult<List<OtherSettingModel>>.ErrorOccured("Setting not found", ResponseCodes.NotFoundErrorCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<List<OtherSettingModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}