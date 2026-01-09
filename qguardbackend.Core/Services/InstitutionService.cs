using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.EmailDtos;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using qguardbackend.Data.Validators;
using examportal.Api.ServiceExtensions;
using examportal.Data.Model;
using LS1_Backend.LS1.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SISService.BoilerPlate.Service.Interfaces;

namespace qguardbackend.Core.Services
{
    public class InstitutionService : IInstitutionService
    {
        private readonly ILogger<InstitutionService> _logger;
        private readonly IS3Service _s3Service;
        private readonly IEmailService _emailService;
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;
        private readonly AppDbContext _context;

        public InstitutionService(AppDbContext context,
            ILogger<InstitutionService> logger,
            IS3Service s3Service,
            IAuthService authService,
            IAuditLogService auditLogService,
            IEmailService emailService)
        {
            _logger = logger;
            _s3Service = s3Service;
            _context = context;
            _emailService = emailService;
            _authService = authService;
            _auditLogService = auditLogService;
        }
        public async Task<CustomResult<InstitutionResponseDto>> Create(InstitutionCreateModel model, string createdBy)
        {

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var validator = new InstitutionValidator();
                    var validationResult = await validator.ValidateAsync(model);
                    if (!validationResult.IsValid)
                    {
                        throw new FluentValidation.ValidationException(validationResult.Errors);
                    }

                    var checkUser = await _context.Users.Where(x => x.Email.ToLower() == model.AdminEmail.ToLower()).FirstOrDefaultAsync();
                    if (checkUser != null)
                        return CustomResult<InstitutionResponseDto>.ErrorOccured("Admin email already exists", ResponseCodes.AlreadyExistErrorCode);

                    if (await _context.Institutions.AnyAsync(x => x.Name.ToLower() == model.Name.ToLower()))
                        return CustomResult<InstitutionResponseDto>.ErrorOccured("Institution name already exists", ResponseCodes.AlreadyExistErrorCode);

                    if (await _context.Institutions.AnyAsync(x => x.Code.ToLower() == model.Code.ToLower()))
                        return CustomResult<InstitutionResponseDto>.ErrorOccured("Institution code already taken", ResponseCodes.AlreadyExistErrorCode);
                    
                    if (await _context.Institutions.AnyAsync(x => x.HostName.ToLower() == model.HostName.ToLower()))
                        return CustomResult<InstitutionResponseDto>.ErrorOccured("Institution host name already taken", ResponseCodes.AlreadyExistErrorCode);

                    string ssoUrl = string.Empty;
                    string logoUrl = string.Empty;

                    if (model.SSORequired == SSORequired.True)
                    {
                        if (string.IsNullOrEmpty(model.SsoURL))
                            return CustomResult<InstitutionResponseDto>.ErrorOccured("SSO URL is required!", ResponseCodes.BadRequestErrorCode);

                        if (model.SsoLogo is null || model.SsoLogo.Length == 0)
                            return CustomResult<InstitutionResponseDto>.ErrorOccured("No SSO logo attached, please upload a valid file", ResponseCodes.BadRequestErrorCode);

                        var ssoResult = await UploadFileAsync(model.SsoLogo);
                        if (!ssoResult.Success)
                            return CustomResult<InstitutionResponseDto>.ErrorOccured(ssoResult.Error, ResponseCodes.BadRequestErrorCode);

                        ssoUrl = ssoResult.Url;
                    }

                    if (model.InstitutionLogo is not null && model.InstitutionLogo.Length > 0)
                    {
                        var logoResult = await UploadFileAsync(model.InstitutionLogo);
                        if (!logoResult.Success)
                            return CustomResult<InstitutionResponseDto>.ErrorOccured(logoResult.Error, ResponseCodes.BadRequestErrorCode);

                        logoUrl = logoResult.Url;
                    }
                    var (first, last) = Helper.SplitFullName(model.AdminName);

                    var institution = Institution.Create(
                        model.Name, model.Code, model.InstitutionType.GetEnumText(), model.AdminName,
                        model.AdminEmail, model.HostName, model.SSORequired.ToString(), model.SenderEmail,
                        model.DefaultLanguage.ToString(), model.PrimaryThemeColor, model.SecondaryThemeColor
                    );

                    institution.SsoURL = ssoUrl;
                    institution.Logo = logoUrl;

                    var createoinstitution = await _context.AddAsync(institution);
                    await _context.SaveChangesAsync();

                    var InstitutionAdminRoleId = await _context.Roles.FirstOrDefaultAsync(x => x.Name.ToLower() == RolesEnum.INSTITUTIONADMIN.GetEnumText().ToLower());
                    //call the user creation service to profile the institution admin as a user 
                    var registerUser = await _authService.RegisterAsync(new RegisterUserRequestDto
                    {
                        Email = model.AdminEmail,
                        FullName = model.AdminName,
                        Password = "Password@123",
                        PhoneNumber = "n/a",
                        FirstName = first,
                        LastName = last,
                        RoleId = InstitutionAdminRoleId.Id,
                        IsActive = true,
                    }, createoinstitution.Entity.Id);

                    //add user to role
                    var addUserToUser = await _context.UserRoles.AddAsync(new ApplicationUserRole
                    {
                        UserId = registerUser.Data.UserId,
                        RoleId = InstitutionAdminRoleId.Id,
                        InstitutionId = createoinstitution.Entity.Id,
                        CreatedAt = DateTime.UtcNow

                    });
                    await  _context.SaveChangesAsync();

                    //add the system admin to have access to the tenant as an institution admin

                    var newUserRolesToAdd = new List<SystemAdminOtherTenantsRole>();

                    var SuperAdminRoleId = await _context.Roles.FirstOrDefaultAsync(x => x.Name.ToLower() == RolesEnum.SYSTEMADMIN.GetEnumText().ToLower());

                    var getSystemAdmin = await _context.UserRoles.Where(x => x.RoleId == SuperAdminRoleId.Id)
                    .ToListAsync();

                    foreach (var systemAdminUser in getSystemAdmin)
                    {
                        bool alreadyExists = await _context.SystemAdminOtherTenantsRole.AnyAsync(x =>
                            x.UserId == systemAdminUser.UserId &&
                            x.RoleId == InstitutionAdminRoleId.Id &&
                            x.InstitutionId == createoinstitution.Entity.Id);

                        if (!alreadyExists)
                        {
                            newUserRolesToAdd.Add(new SystemAdminOtherTenantsRole
                            {
                                UserId = systemAdminUser.UserId,
                                RoleId = InstitutionAdminRoleId.Id,
                                InstitutionId = createoinstitution.Entity.Id,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    if (newUserRolesToAdd.Any())
                    {
                        await _context.SystemAdminOtherTenantsRole.AddRangeAsync(newUserRolesToAdd);
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await _auditLogService.AddToAudit((int)AuditActionType.Create, "Institution", $"User [{createdBy}] created an institution - {model.Name} at {DateTime.UtcNow}.");

                    //TODO: send email to the Institution admin user with the password
                    if (registerUser.IsSuccess)
                    {
                        await _emailService.SendWelcomeEmailAsync(new SendWelcomeEmailVM
                        {
                            Fullname = model.AdminName,
                            Password = "Password@123", //TODO: Replace with  Utility.CreateRandomPasswordWithRandomLength(),
                            receiverEmail = model.AdminEmail,
                            InstitutionBaseUrl = $"{createoinstitution.Entity.HostName}/auth/login",
                        });
                    }
                    return await GetById(institution.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, ex.Message);
                    return CustomResult<InstitutionResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
                }
            });
        }

        private async Task<(bool Success, string Url, string Error)> UploadFileAsync(IFormFile file, string fileName = null)
        {
            var supportedTypes = new[] { "jpg", "jpeg", "png", "svg" };
            const int maxFileSize = 2_000_000; // 2MB

            if (file.Length > maxFileSize)
                return (false, null, "File is too large, 2MB max!");

            var fileExt = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
            if (!supportedTypes.Contains(fileExt))
                return (false, null, "Only SVG/PNG/JPEG/JPG files are allowed!");

            fileName ??= $"{DateTime.UtcNow:ddssmm}_{file.FileName}";
            var result = await _s3Service.UploadToS3Async(file, fileName);

            return result.Success
                ? (true, result.FileName, null)
                : (false, null, "Failed to upload file, something went wrong.");
        }

        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            try
            {
                var query = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                {
                    return CustomResult<string>.ErrorOccured("Institution not found!", ResponseCodes.NotFoundErrorCode);
                }
                query.IsActive = false;
                query.UpdatedAt = DateTime.UtcNow;
                _context.Update(query);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Delete, "Institution", $"User [{createdBy}] deleted an institution - {query.Name} at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Institution successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<InstitutionResponseDto>>> GetAll(InstitutionFilterModel search)
        {
            try
            {

                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Institution> records = _context.Institutions;

                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.Name.ToLower().Contains(searchWord) ||
                        x.Code.ToLower().Contains(searchWord) ||
                        x.AdminEmail.ToLower().Contains(searchWord) ||
                        x.HostName.ToLower().Contains(searchWord) ||
                        x.AdminName.ToLower().Contains(searchWord) ||
                        x.PrimaryThemeColor.ToLower().Contains(searchWord) ||
                        x.SecondaryThemeColor.ToLower().Contains(searchWord) ||
                        x.InstitutionType.ToLower().Contains(searchWord));
                }

                if (search.Status.HasValue)
                {
                    bool status = search.Status.Value;
                    records = records.Where(x => x.IsActive == status);
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
                var result = await records.Select(query => new InstitutionResponseDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    Code = query.Code,
                    AdminName = query.AdminName,
                    AdminEmail = query.AdminEmail,
                    HostName = query.HostName,
                    Type = query.InstitutionType,
                    Logo = string.IsNullOrEmpty(query.Logo) ? "n/a" : query.Logo,
                    SenderEmail = query.SenderEmail,
                    SsoURL = string.IsNullOrEmpty(query.SsoURL) ? "n/a" : query.SsoURL,
                    SsoLogo = string.IsNullOrEmpty(query.SsoLogo) ? "n/a" : query.SsoLogo,
                    SSORequired = query.SSORequired,
                    DefaultLanguage = query.DefaultLanguage,
                    PrimaryThemeColor = query.PrimaryThemeColor,
                    SecondaryThemeColor = query.SecondaryThemeColor,
                    Status = (query.IsActive) ? "Active" : "Inactive",
                    DateCreated = query.CreatedAt
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<InstitutionResponseDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<InstitutionResponseDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<InstitutionResponseDto>> GetByHostName(string hostname)
        {
            try
            {
                var query = await _context.Institutions
                                        .FirstOrDefaultAsync(c => c.HostName.ToLower() == hostname.ToLower());
                if (query == null)
                {
                    return CustomResult<InstitutionResponseDto>.ErrorOccured("Invalid Institution hostname!", ResponseCodes.BadRequestErrorCode);
                }
                var model = new InstitutionResponseDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    Code = query.Code,
                    AdminName = query.AdminName,
                    AdminEmail = query.AdminEmail,
                    HostName = query.HostName,
                    Status = (query.IsActive) ? "Active" : "Inactive",
                    Type = query.InstitutionType,
                    Logo = string.IsNullOrEmpty(query.Logo) ? "n/a" : query.Logo,
                    SenderEmail = query.SenderEmail,
                    SsoURL = string.IsNullOrEmpty(query.SsoURL) ? "n/a" : query.SsoURL,
                    SsoLogo = string.IsNullOrEmpty(query.SsoLogo) ? "n/a" : query.SsoLogo,
                    SSORequired = query.SSORequired,
                    DefaultLanguage = query.DefaultLanguage,
                    PrimaryThemeColor = query.PrimaryThemeColor,
                    SecondaryThemeColor = query.SecondaryThemeColor,
                    DateCreated = query.CreatedAt
                };
                return CustomResult<InstitutionResponseDto>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<InstitutionResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
        public async Task<CustomResult<InstitutionResponseDto>> GetByCode(string code)
        {
            try
            {
                var query = await _context.Institutions
                                        .FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower());
                if (query == null)
                {
                    return CustomResult<InstitutionResponseDto>.ErrorOccured("Invalid Institution code!", ResponseCodes.BadRequestErrorCode);
                }
                var model = new InstitutionResponseDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    Code = query.Code,
                    AdminName = query.AdminName,
                    AdminEmail = query.AdminEmail,
                    HostName = query.HostName,
                    Status = (query.IsActive) ? "Active" : "Inactive",
                    Type = query.InstitutionType,
                    Logo = string.IsNullOrEmpty(query.Logo) ? "n/a" : query.Logo,
                    SenderEmail = query.SenderEmail,
                    SsoURL = string.IsNullOrEmpty(query.SsoURL) ? "n/a" : query.SsoURL,
                    SsoLogo = string.IsNullOrEmpty(query.SsoLogo) ? "n/a" : query.SsoLogo,
                    SSORequired = query.SSORequired,
                    DefaultLanguage = query.DefaultLanguage,
                    PrimaryThemeColor = query.PrimaryThemeColor,
                    SecondaryThemeColor = query.SecondaryThemeColor,
                    DateCreated = query.CreatedAt
                };
                return CustomResult<InstitutionResponseDto>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<InstitutionResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<InstitutionResponseDto>> GetById(long id)
        {
            try
            {
                var query = await _context.Institutions
                                    .FirstOrDefaultAsync(c => c.Id == id);
                if (query == null)
                {
                    return CustomResult<InstitutionResponseDto>.ErrorOccured("Invalid Institution id!", ResponseCodes.BadRequestErrorCode);
                }
                var model = new InstitutionResponseDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    Code = query.Code,
                    AdminName = query.AdminName,
                    AdminEmail = query.AdminEmail,
                    HostName = query.HostName,
                    Status = (query.IsActive) ? "Active" : "Inactive",
                    Type = query.InstitutionType,
                    Logo = string.IsNullOrEmpty(query.Logo) ? "n/a" : query.Logo,
                    SenderEmail = query.SenderEmail,
                    SsoURL = string.IsNullOrEmpty(query.SsoURL) ? "n/a" : query.SsoURL,
                    SsoLogo = string.IsNullOrEmpty(query.SsoLogo) ? "n/a" : query.SsoLogo,
                    SSORequired = query.SSORequired,
                    DefaultLanguage = query.DefaultLanguage,
                    PrimaryThemeColor = query.PrimaryThemeColor,
                    SecondaryThemeColor = query.SecondaryThemeColor,
                    DateCreated = query.CreatedAt
                };
                return CustomResult<InstitutionResponseDto>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<InstitutionResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task SeedDefaultInstitution()
        {
            if (_context.Institutions.Any())
            {
                return;
            }
            var institution = Institution.Create(
                    "MASTER INSTITUTION", "MASTER", InstitutionTypesEnum.SCHOOL.GetEnumText(), "SYSTEM ADMIN",
                    "daniel.ogwu@vigilearn.com", "https://school1examportal.vigilearn.com", SSORequired.False.ToString(),
                    "noreply@vigilearn.com", LanguageType.ENGLISH.ToString(), "#3498db", "#07bc0c"
                );
            await _context.AddAsync(institution);
            await _context.SaveChangesAsync();
        }

        public async Task<CustomResult<InstitutionResponseDto>> Update(long id, InstitutionCreateModel model, string createdBy)
        {
            try
            {
                var validator = new InstitutionValidator();
                var validationResult = await validator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    throw new FluentValidation.ValidationException(validationResult.Errors);
                }

                var institution = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == id);
                if (institution is null)
                {
                    return CustomResult<InstitutionResponseDto>.ErrorOccured("Institution ID does not exist!", ResponseCodes.BadRequestErrorCode);
                }

                if (model.SSORequired == SSORequired.True)
                {
                    if (string.IsNullOrEmpty(model.SsoURL))
                    {
                        return CustomResult<InstitutionResponseDto>.ErrorOccured("SSO URL is required!", ResponseCodes.BadRequestErrorCode);
                    }

                    if (model.SsoLogo is not null && model.SsoLogo.Length > 0)
                    {
                        var ssoUpload = await UploadFileAsync(model.SsoLogo);
                        if (!ssoUpload.Success)
                        {
                            return CustomResult<InstitutionResponseDto>.ErrorOccured(ssoUpload.Error, ResponseCodes.BadRequestErrorCode);
                        }
                        institution.SsoURL = model.SsoURL;
                        institution.Logo = ssoUpload.Url;
                    }
                }

                if (model.InstitutionLogo is not null && model.InstitutionLogo.Length > 0)
                {
                    var logoUpload = await UploadFileAsync(model.InstitutionLogo);
                    if (!logoUpload.Success)
                    {
                        return CustomResult<InstitutionResponseDto>.ErrorOccured(logoUpload.Error, ResponseCodes.BadRequestErrorCode);
                    }
                    institution.Logo = logoUpload.Url;
                }

                institution.Name = model.Name;
                institution.Code = model.Code;
                institution.InstitutionType = model.InstitutionType.GetEnumText();
                institution.SenderEmail = model.SenderEmail;
                institution.AdminEmail = model.AdminEmail;
                institution.AdminName = model.AdminName;
                institution.DefaultLanguage = model.DefaultLanguage.ToString();
                institution.HostName = model.HostName;
                institution.PrimaryThemeColor = model.PrimaryThemeColor;
                institution.SecondaryThemeColor = model.SecondaryThemeColor;
                institution.UpdatedAt = DateTime.UtcNow;

                _context.Institutions.Update(institution);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Institution", $"User [{createdBy}] update an institution to - {model.Name} at {DateTime.UtcNow}.");

                return await GetById(institution.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<InstitutionResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<bool> CheckinstitutionByCode(string code)
        {
            if (await _context.Institutions.AnyAsync(x => x.Code.ToLower() == code.ToLower()))
            {
                return true;
            }
            return false;
        }

        public async Task<CustomResult<InstitutionResponseDto>> AddInstitutionLogo(long id, IFormFile formData)
        {
            try
            {
                if (formData is null || formData.Length == 0)
                {
                    return CustomResult<InstitutionResponseDto>.ErrorOccured("No logo was attached, please upload a valid file", ResponseCodes.BadRequestErrorCode);
                }

                var institution = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == id);
                if (institution is null)
                {
                    return CustomResult<InstitutionResponseDto>.ErrorOccured("Institution ID does not exist!", ResponseCodes.BadRequestErrorCode);
                }

                // Comment this because of resource doesn't have permission to delete
                //if (!string.IsNullOrWhiteSpace(institution.Logo))
                //{
                //    var delete = await _s3Service.DeleteFileFromS3Async(Path.GetFileName(institution.Logo));
                //    if (!delete.Success)
                //    {
                //        return CustomResult<InstitutionResponseDto>.ErrorOccured($"Error removing old logo: {delete.FileName}", ResponseCodes.BadRequestErrorCode);
                //    }
                //}

                var fileName = $"{institution.Code}_{formData.FileName}";
                var upload = await UploadFileAsync(formData, fileName);
                if (!upload.Success)
                {
                    return CustomResult<InstitutionResponseDto>.ErrorOccured(upload.Error, ResponseCodes.BadRequestErrorCode);
                }

                institution.Logo = upload.Url;
                _context.Institutions.Update(institution);
                await _context.SaveChangesAsync();

                return await GetById(institution.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<InstitutionResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> ChangeInstitutionStatus(long id, string createdBy)
        {
            try
            {
                var record = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == id);
                if (record == null)
                {
                    return CustomResult<string>.ErrorOccured("Institution not found!", ResponseCodes.NotFoundErrorCode);
                }

                record.IsActive = !record.IsActive;
                _context.Institutions.Update(record);
                await _context.SaveChangesAsync();
                string message = (record.IsActive) ? "enabled" : "disabled";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Institution", $"User [{createdBy}], {message} an institution at {DateTime.UtcNow}.");

                if (record.IsActive == true)
                {
                    return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Institution enabled successfully");
                }
                else
                {
                    return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Institution disabled successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}