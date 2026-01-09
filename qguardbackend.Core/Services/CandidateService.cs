using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;
using qguardbackend.Core.BackGroundService;
using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.EmailDtos;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using qguardbackend.Data.Validators;
using examportal.Api.ServiceExtensions;
using examportal.Data.Entities;
using examportal.Data.Model;
using FluentEmail.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SISService.BoilerPlate.Service.Interfaces;
using System.Text;

namespace qguardbackend.Core.Services
{
    public class CandidateService : ICandidateService
    {

        private readonly ILogger<CandidateService> _logger;
        private readonly IS3Service _s3Service;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmailService _emailService;
        private readonly IAuthService _authService;
        private readonly IUserManagementService _userManagementService;
        private readonly AppDbContext _context;
        private readonly IBackgroundEmailQueue _backgroundEmailQueue;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        public CandidateService(ILogger<CandidateService> logger,
             IUserManagementService userManagementService,
             IAuthService authService,
             IEmailService emailService,
             IS3Service s3Service,
             IAuditLogService auditLogService,
             UserManager<ApplicationUser> userManager,
             RoleManager<ApplicationRole> roleManager,
             AppDbContext context,
             IBackgroundEmailQueue backgroundEmailQueue)
        {
            _authService = authService;
            _logger = logger;
            _s3Service = s3Service;
            _auditLogService = auditLogService;
            _context = context;
            _backgroundEmailQueue = backgroundEmailQueue;
            _emailService = emailService;
            _userManagementService = userManagementService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<CustomResult<CreateUpdateCandidateResponseDto>> Create(CandidateRequestDto model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var InsTId = await _userManagementService.GetTenantId();
                    if (!InsTId.IsSuccess)
                    {
                        return CustomResult<CreateUpdateCandidateResponseDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                    }
                    var validator = new CandidateValidator();
                    var validationResult = await validator.ValidateAsync(model);
                    if (!validationResult.IsValid)
                    {
                        throw new FluentValidation.ValidationException(validationResult.Errors);
                    }
                    var GetInstitution = await _context.Institutions
                                                .FirstOrDefaultAsync(x => x.Id == InsTId.Data);

                    if (GetInstitution is null)
                        return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured("Institution does not exists", ResponseCodes.NotFoundErrorCode);

                    var newCandidate = new Candidate();

                    var checkUser = await _context.Users.Where(x => x.Email.ToLower() == model.Email.ToLower() && x.InstitutionId == InsTId.Data).FirstOrDefaultAsync();
                    if (checkUser != null)
                    {
                        var IsCandidateInSameInstitution = await _context.UserRoles
                        .FirstOrDefaultAsync(x => x.UserId == checkUser.Id && x.InstitutionId == InsTId.Data);

                        if (IsCandidateInSameInstitution is not null)
                        {
                            return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured("Candidate is already existing in the same Institution", ResponseCodes.AlreadyExistErrorCode);
                        }
                    }

                    var isMatricNumberExist = await _context.Candidates.AnyAsync(x => x.MatricNumber.ToLower() == model.MatricNumber.ToLower() && x.InstitutionId == InsTId.Data);
                    if (isMatricNumberExist)
                        return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured($"Matric number - {model.MatricNumber} already exist for another student", ResponseCodes.NotFoundErrorCode);

                    var country = await _context.Countries.FirstOrDefaultAsync(x => x.Id == model.CountryId);
                    if (country is null)
                        return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured($"Country text filed is required!", ResponseCodes.NotFoundErrorCode);

                    var region = await _context.Regions.FirstOrDefaultAsync(x => x.Id == model.RegionId && x.Country_Id == model.CountryId);
                    if (region is null)
                        return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured($"State selected does not belong to the country picked!", ResponseCodes.NotFoundErrorCode);

                    var city = await _context.Cities.FirstOrDefaultAsync(x => x.Id == model.CityId && x.Region_Id == model.RegionId);
                    if (city is null)
                        return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured($"City selected does not belong to the state picked!", ResponseCodes.NotFoundErrorCode);

                    if (model.Passport is not null && model.Passport.Length > 0)
                    {
                        var logoResult = await UploadFileAsync(model.Passport);
                        if (!logoResult.Success)
                            return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured(logoResult.Error, ResponseCodes.BadRequestErrorCode);

                        newCandidate.PicturePath = logoResult.Url;
                    }
                    //build other parameters on the candidate`

                    newCandidate.FacultyId = model.FacultyId;
                    newCandidate.FacultyId = model.FacultyId;
                    newCandidate.DepartmentId = model.DepartmentId;
                    newCandidate.ProgramId = model.ProgramId;
                    newCandidate.SessionId = model.SessionId;
                    newCandidate.InstitutionId = InsTId.Data;
                    newCandidate.SemesterId = model.SemesterId;
                    newCandidate.PhoneNumber = model.PhoneNumber;
                    newCandidate.DateOfBirth = model.DateOfBirth;
                    newCandidate.Gender = model.Gender;
                    newCandidate.LevelId = model.LevelId;
                    newCandidate.IsActive = true;
                    newCandidate.MatricNumber = model.MatricNumber;
                    newCandidate.RegistrationNumber = model.RegistrationNumber;
                    newCandidate.CountryId = country.Id;
                    newCandidate.RegionId = region.Id;
                    newCandidate.CityId = city.Id;

                    var NewPassword = model.LastName.Replace(" ","").Trim() + "@123";

                    var candidateroleId = _authService.GetRoleIdbyRoleName(RolesEnum.CANDIDATE.GetEnumText()).Result;

                    var registerUser = await _authService.RegisterAsync(new RegisterUserRequestDto
                    {
                        Email = model.Email,
                        FullName = $"{model.LastName} {model.FirstName}",
                        Password = NewPassword,
                        PhoneNumber = model.PhoneNumber,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        RoleId = candidateroleId,
                        IsActive = false
                    }, InsTId.Data);

                    newCandidate.UserId = registerUser.Data.UserId;

                    var addUserAsSystemAdmin = await _context.UserRoles.AddAsync(new ApplicationUserRole
                    {
                        UserId = newCandidate.UserId,
                        RoleId = candidateroleId, 
                        InstitutionId = InsTId.Data
                    });
                    await _context.SaveChangesAsync();

                    var NewCand = await _context.Candidates.AddAsync(newCandidate);
                    await _context.SaveChangesAsync();

                    //capture the current state of the candidate on onboarding, subsequently the student will current state will be updated by the Institution Admin
                    var StudenState = new CandidatesCurrentState
                    {
                        CandidateId = NewCand.Entity.Id,
                        CreatedAt = DateTime.UtcNow,
                        LevelId = model.LevelId,
                        SemesterId = model.SemesterId,
                        SessionId = model.SessionId,
                        IsActive = true,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false,
                        IsCurrent = true,
                        InstitutionId = InsTId.Data
                    };
                    await _context.CandidatesCurrentStates.AddAsync(StudenState);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await _auditLogService.AddToAudit((int)AuditActionType.Create, "Candidate", $"User [{model.LastName} {model.FirstName}] sign up at exactly {DateTime.UtcNow}.");
                    _logger.LogInformation($"new User to create with email: {model.Email}, Default Password: {NewPassword}");
                    if (registerUser.IsSuccess)
                    {
                        //var NewOtp = await _authService.GenerateNewOTP(registerUser.Data.UserId);
                        _ = Task.Run(async () =>
                        {
                            await _emailService.SendWelcomeEmailAsync(new SendWelcomeEmailVM
                            {
                                InstitutionBaseUrl = $"{GetInstitution.HostName}/auth/login",
                                Fullname = $"{model.LastName} {model.FirstName}",
                                Password = NewPassword,
                                receiverEmail = model.Email,
                            });
                        });
                    }
                    return CustomResult<CreateUpdateCandidateResponseDto>.Success(new CreateUpdateCandidateResponseDto
                    {
                        CandidateId = NewCand.Entity.Id,
                        UserId = newCandidate.UserId,
                        IsCreatedOrUpdated = true
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, ex.Message);
                    return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
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

        public async Task<CustomResult<bool>> Delete(long id, string createdBy)
        {
            try
            {
                var candiateDetails = await _context.Candidates.FirstOrDefaultAsync(x => x.Id == id);
                if (candiateDetails == null)
                {
                    return CustomResult<bool>.ErrorOccured("Candidate not found!", ResponseCodes.NotFoundErrorCode);
                }
                candiateDetails.IsDeleted = true;
                _context.Candidates.Update(candiateDetails);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Delete, "Candidate", $"User [{createdBy}] deleted candidate with matric number - {candiateDetails.MatricNumber} account at exactly {DateTime.UtcNow}.");
                return CustomResult<bool>.Success(true, "Deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<bool>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
        public async Task<CustomResult<string>> EnableDisableCandidate(long id, string createdBy)
        {
            try
            {
                var candidateDetails = await _context.Candidates.FirstOrDefaultAsync(x => x.Id == id);
                if (candidateDetails == null)
                {
                    return CustomResult<string>.ErrorOccured("Candidate not found!", ResponseCodes.NotFoundErrorCode);
                }
                candidateDetails.IsActive = !candidateDetails.IsActive;
                _context.Candidates.Update(candidateDetails);

                //change the status of the user 
                var getUser = await _context.Users.FirstOrDefaultAsync(x => x.Id == candidateDetails.UserId);
                getUser.IsActive = !getUser.IsActive;
                _context.Users.Update(getUser);


                await _context.SaveChangesAsync();
                string message = (candidateDetails.IsActive) ? "enabled" : "disabled";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Candidate", $"User [{createdBy}], {message} candidate with matric number - {candidateDetails.MatricNumber} account at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, ResponseMessages.UserStatusChanged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<CreateUpdateCandidateResponseDto>> Update(long id, UpdateCandidateRequestDto model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var validator = new UpdateCandidateValidator();
                var validationResult = await validator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured(
                        string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        ResponseCodes.BadRequestErrorCode
                    );
                }

                var institution = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == model.InstitutionId);
                if (institution is null)
                    return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured("Institution does not exists", ResponseCodes.NotFoundErrorCode);

                var CandidateDetails = await _context.Candidates.FirstOrDefaultAsync(x => x.Id == id);
                if (CandidateDetails is null)
                    return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured(ResponseMessages.CandidateDetailsNotFound, ResponseCodes.NotFoundErrorCode);

                var isMatricNumberExist = await _context.Candidates.AnyAsync(x => x.MatricNumber.ToLower() == model.MatricNumber.ToLower()
                        && x.MatricNumber.ToLower() != CandidateDetails.MatricNumber.ToLower()
                        && x.InstitutionId == model.InstitutionId);
                if (isMatricNumberExist)
                    return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured($"Matric number - {model.MatricNumber} already exist for another student", ResponseCodes.NotFoundErrorCode);

                var country = await _context.Countries.FirstOrDefaultAsync(x=>x.Id == model.CountryId);
                if(country is null)
                    return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured($"Country text filed is required!", ResponseCodes.NotFoundErrorCode);

                var region = await _context.Regions.FirstOrDefaultAsync(x => x.Id == model.RegionId && x.Country_Id == model.CountryId);
                if (region is null)
                    return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured($"State selected does not belong to the country picked!", ResponseCodes.NotFoundErrorCode);

                var city = await _context.Cities.FirstOrDefaultAsync(x => x.Id == model.CityId && x.Region_Id == model.RegionId);
                if (city is null)
                    return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured($"City selected does not belong to the state picked!", ResponseCodes.NotFoundErrorCode);

                if (model.Passport is not null && model.Passport.Length > 0)
                {
                    var logoResult = await UploadFileAsync(model.Passport);
                    if (!logoResult.Success)
                        return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured(logoResult.Error, ResponseCodes.BadRequestErrorCode);

                    CandidateDetails.PicturePath = logoResult.Url;
                }
                //build other parameters on the candidate`
                CandidateDetails.FacultyId = model.FacultyId;
                CandidateDetails.DepartmentId = model.DepartmentId;
                CandidateDetails.ProgramId = model.ProgramId;
                CandidateDetails.SessionId = model.SessionId;
                CandidateDetails.SemesterId = model.SemesterId;
                CandidateDetails.PhoneNumber = model.PhoneNumber;
                CandidateDetails.DateOfBirth = model.DateOfBirth;
                CandidateDetails.LevelId = model.LevelId;
                CandidateDetails.MatricNumber = model.MatricNumber;
                CandidateDetails.CountryId = country.Id;
                CandidateDetails.RegionId = region.Id;
                CandidateDetails.CityId = city.Id;
                CandidateDetails.Gender = model.Gender;

                //if the InstitutionCode is different then update the User-role-Institution Table
                var userRole = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserId == CandidateDetails.UserId);
                if (userRole != null && userRole.InstitutionId != model.InstitutionId)
                {
                    userRole.InstitutionId = model.InstitutionId;
                    _context.UserRoles.Update(userRole);
                }

                _context.Candidates.Update(CandidateDetails);

                var userDetails = await _context.Users.FirstOrDefaultAsync(x => x.Id == CandidateDetails.UserId);

                userDetails.FirstName = model.FirstName;
                userDetails.LastName = model.LastName;

                _context.Users.Update(userDetails);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Candidate", $"User [{CandidateDetails.MatricNumber}] update a his/her profile at {DateTime.UtcNow}.");

                return CustomResult<CreateUpdateCandidateResponseDto>.Success(new CreateUpdateCandidateResponseDto
                {
                    CandidateId = id,
                    UserId = CandidateDetails.UserId,
                    IsCreatedOrUpdated = true
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, ex.Message);
                return CustomResult<CreateUpdateCandidateResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<UpdateCandidateStatuResponseDto>> UpdateCandidateState(UpdateCandidateStatusRequestDto model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<UpdateCandidateStatuResponseDto>.Failure(
                        CustomError.InvalidTenant,
                        ResponseCodes.InvalidTenant
                    );
                }

                var institutionId = InsTId.Data;
                var errors = new List<string>();
                var validCandidates = new List<Candidate>();

                // Fetch candidates to update
                var candidates = await _context.Candidates
                    .Where(x => model.CandidatesId.Contains(x.Id))
                    .ToListAsync();

                foreach (var c in candidates)
                {
                    // Validate Level
                    if (!await _context.Levels.AnyAsync(x => x.Id == model.LevelId))
                    {
                        errors.Add($"Invalid LevelId ({model.LevelId}) for CandidateId {c.Id}");
                        continue;
                    }

                    // Validate Session
                    if (!await _context.Sessions.AnyAsync(x => x.Id == model.SessionId))
                    {
                        errors.Add($"Invalid SessionId ({model.SessionId}) for CandidateId {c.Id}");
                        continue;
                    }

                    // Validate Semester
                    if (!await _context.Semesters.AnyAsync(x => x.Id == model.SemesterId))
                    {
                        errors.Add($"Invalid SemesterId ({model.SemesterId}) for CandidateId {c.Id}");
                        continue;
                    }
                    c.LevelId = model.LevelId;
                    c.SessionId = model.SessionId;
                    c.SemesterId = model.SemesterId;
                    c.UpdatedAt = DateTime.UtcNow;
                    c.InstitutionId = institutionId;

                    validCandidates.Add(c);
                }

                if (errors.Any())
                {
                    return CustomResult<UpdateCandidateStatuResponseDto>.ErrorOccured(
                        "Validation failed for some candidates: " + string.Join(" | ", errors),
                        ResponseCodes.OperationError
                    );
                }

                // Only update valid candidates
                _context.Candidates.UpdateRange(validCandidates);
                await _context.SaveChangesAsync();

                // Fetch existing candidate states (IMPORTANT FIX: use CandidateId)
                var existingStates = await _context.CandidatesCurrentStates
                    .Where(x => model.CandidatesId.Contains(x.CandidateId))
                    .ToListAsync();

                // Mark all existing states inactive
                foreach (var state in existingStates)
                {
                    state.IsActive = false;
                    state.IsCurrent = false;
                }

                _context.CandidatesCurrentStates.UpdateRange(existingStates);
                await _context.SaveChangesAsync();

                // Insert or update new candidate states
                int savedCount = 0;

                foreach (var candidate in candidates)
                {
                    var existing = await _context.CandidatesCurrentStates.FirstOrDefaultAsync(x =>
                        x.CandidateId == candidate.Id &&
                        x.SessionId == model.SessionId &&
                        x.SemesterId == model.SemesterId &&
                        x.LevelId == model.LevelId
                    );

                    if (existing != null)
                    {
                        existing.IsActive = true;
                        existing.IsCurrent = true;
                        existing.UpdatedAt = DateTime.UtcNow;

                        _context.CandidatesCurrentStates.Update(existing);
                    }
                    else
                    {
                        var newState = new CandidatesCurrentState
                        {
                            CandidateId = candidate.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            LevelId = model.LevelId!.Value,
                            SemesterId = model.SemesterId!.Value,
                            SessionId = model.SessionId!.Value,
                            IsActive = true,
                            IsCurrent = true,
                            IsDeleted = false,
                            InstitutionId = institutionId
                        };

                        await _context.CandidatesCurrentStates.AddAsync(newState);
                    }

                    savedCount += await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Audit Log
                await _auditLogService.AddToAudit(
                    (int)AuditActionType.Create,
                    "Candidate State Update",
                    $"{savedCount} candidate(s) moved to another level, semester and session at {DateTime.UtcNow}."
                );

                return CustomResult<UpdateCandidateStatuResponseDto>.Success(new UpdateCandidateStatuResponseDto
                {
                    Message = $"{savedCount} Candidate(s) have been updated successfully.",
                    Status = savedCount > 0
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, ex.Message);
                return CustomResult<UpdateCandidateStatuResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<CandidateModel>>> GetCandidatesByInstitutionId(long id, CandidateFilterModel search)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Candidate> records = _context.Candidates
                                                .Include(x => x.User).ThenInclude(x => x.Institution)
                                                .Include(x => x.Faculty)
                                                .Include(x => x.Department)
                                                .Include(x => x.Program)
                                                .Include(x => x.Level)
                                                .Include(x => x.Semester)
                                                .Include(x => x.Session)
                                                .Where(x => x.User.InstitutionId == id);

                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.User.FirstName.ToLower().Contains(searchWord) ||
                        x.User.LastName.Contains(searchWord) ||
                        x.User.Email.ToLower().Contains(searchWord) ||
                        x.MatricNumber.ToLower().Contains(searchWord) ||
                        x.PhoneNumber.ToLower().Contains(searchWord) ||
                        x.Gender.ToLower().Contains(searchWord) ||
                        x.RegistrationNumber.ToLower().Contains(searchWord)
                    );
                }
                if (!string.IsNullOrEmpty(search.Name))
                {
                    records = records.Where(x => x.User.FirstName.ToLower().Contains(search.Name.ToLower()) ||
                                x.User.LastName.ToLower().Contains(search.Name.ToLower()));
                }
                if (!string.IsNullOrEmpty(search.RegNo))
                {
                    records = records.Where(x => x.RegistrationNumber.ToLower() == search.RegNo.ToLower());
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= search.StartDate && x.CreatedAt <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.User.FirstName);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }
                var result = await records.Select(query => new CandidateModel
                {
                    Id = query.Id,
                    FirstName = query.User.FirstName,
                    LastName = query.User.LastName,
                    Email = query.User.Email,
                    PhoneNumber = query.PhoneNumber,
                    DateOfBirth = query.DateOfBirth.Value,
                    Gender = query.Gender,
                    MatricNumber = query.MatricNumber,
                    PicturePath = string.IsNullOrEmpty(query.PicturePath) ? "" : query.PicturePath,
                    Level = query.Level.LevelName,
                    Semester = query.Semester == null ? null : new SemesterModel
                    {
                        Id = query.Semester.Id,
                        Name = query.Semester.SemesterName,
                        DateCreated = query.Semester.CreatedAt
                    },
                    Session = query.Session == null ? null : new SessionDto
                    {
                        Id = query.Session.Id,
                        Name = query.Session.Name,
                        DateCreated = query.Session.CreatedAt
                    },
                    Department = query.Department == null ? null : new DepartmentDto
                    {
                        Id = query.Department.Id,
                        Name = query.Department.Name,
                        DateCreated = query.Department.CreatedAt
                    },
                    Faculty = query.Faculty == null ? null : new FacultyDto
                    {
                        Id = query.Faculty.Id,
                        Name = query.Faculty.Name,
                        DateCreated = query.Faculty.CreatedAt
                    },
                    Program = query.Program == null ? null : new ProgramModel
                    {
                        Id = query.Program.Id,
                        Name = query.Program.Name,
                        DateCreated = query.Program.CreatedAt
                    },
                    Institution = query.User.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = query.User.Institution.Id,
                        Name = query.User.Institution.Name,
                        Code = query.User.Institution.Code,
                        AdminName = query.User.Institution.AdminName,
                        AdminEmail = query.User.Institution.AdminEmail,
                        HostName = query.User.Institution.HostName,
                        Type = query.User.Institution.InstitutionType,
                        Logo = string.IsNullOrEmpty(query.User.Institution.Logo) ? "n/a" : query.User.Institution.Logo,
                        SenderEmail = query.User.Institution.SenderEmail,
                        SsoURL = string.IsNullOrEmpty(query.User.Institution.SsoURL) ? "n/a" : query.User.Institution.SsoURL,
                        SsoLogo = string.IsNullOrEmpty(query.User.Institution.SsoLogo) ? "n/a" : query.User.Institution.SsoLogo,
                        SSORequired = query.User.Institution.SSORequired,
                        DefaultLanguage = query.User.Institution.DefaultLanguage,
                        PrimaryThemeColor = query.User.Institution.PrimaryThemeColor,
                        SecondaryThemeColor = query.User.Institution.SecondaryThemeColor,
                        Status = (query.User.Institution.IsActive) ? "Active" : "Inactive",
                        DateCreated = query.User.Institution.CreatedAt
                    }
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<CandidateModel>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<CandidateModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<CandidateModel>>> GetCandidatesByInstitutionCode(string code, CandidateFilterModel search)
        {
            try
            {
                var institution = await _context.Institutions.FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower());
                if (institution == null)
                {
                    return CustomResult<PaginatedResult<CandidateModel>>.ErrorOccured("Invalid Institution code!", ResponseCodes.BadRequestErrorCode);
                }
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Candidate> records = _context.Candidates
                                                .Include(x => x.User).ThenInclude(x => x.Institution)
                                                .Include(x => x.Faculty)
                                                .Include(x => x.Level)
                                                .Include(x => x.Department)
                                                .Include(x => x.Program)
                                                .Include(x => x.Semester)
                                                .Include(x => x.Session)
                                                .Where(x => x.User.Institution.Code.ToLower() == institution.Code.ToLower());

                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.User.FirstName.ToLower().Contains(searchWord) ||
                        x.User.LastName.Contains(searchWord) ||
                        x.User.Email.ToLower().Contains(searchWord) ||
                        x.MatricNumber.ToLower().Contains(searchWord) ||
                        x.PhoneNumber.ToLower().Contains(searchWord) ||
                        x.Gender.ToLower().Contains(searchWord) ||
                        x.RegistrationNumber.ToLower().Contains(searchWord));
                }
                if (!string.IsNullOrEmpty(search.Name))
                {
                    records = records.Where(x => x.User.FirstName.ToLower().Contains(search.Name.ToLower()) ||
                                x.User.LastName.ToLower().Contains(search.Name.ToLower()));
                }
                if (!string.IsNullOrEmpty(search.RegNo))
                {
                    records = records.Where(x => x.RegistrationNumber.ToLower() == search.RegNo.ToLower());
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= search.StartDate && x.CreatedAt <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.User.FirstName);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }
                var result = await records.Select(query => new CandidateModel
                {
                    Id = query.Id,
                    FirstName = query.User.FirstName,
                    LastName = query.User.LastName,
                    Email = query.User.Email,
                    PhoneNumber = query.PhoneNumber,
                    DateOfBirth = query.DateOfBirth.Value,
                    Gender = query.Gender,
                    MatricNumber = query.MatricNumber,
                    PicturePath = string.IsNullOrEmpty(query.PicturePath) ? "" : query.PicturePath,
                    Level = query.Level.LevelName,
                    Semester = query.Semester == null ? null : new SemesterModel
                    {
                        Id = query.Semester.Id,
                        Name = query.Semester.SemesterName,
                        DateCreated = query.Semester.CreatedAt
                    },
                    Session = query.Session == null ? null : new SessionDto
                    {
                        Id = query.Session.Id,
                        Name = query.Session.Name,
                        DateCreated = query.Session.CreatedAt
                    },
                    Department = query.Department == null ? null : new DepartmentDto
                    {
                        Id = query.Department.Id,
                        Name = query.Department.Name,
                        DateCreated = query.Department.CreatedAt
                    },
                    Faculty = query.Faculty == null ? null : new FacultyDto
                    {
                        Id = query.Faculty.Id,
                        Name = query.Faculty.Name,
                        DateCreated = query.Faculty.CreatedAt
                    },
                    Program = query.Program == null ? null : new ProgramModel
                    {
                        Id = query.Program.Id,
                        Name = query.Program.Name,
                        DateCreated = query.Program.CreatedAt
                    },
                    Institution = query.User.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = query.User.Institution.Id,
                        Name = query.User.Institution.Name,
                        Code = query.User.Institution.Code,
                        AdminName = query.User.Institution.AdminName,
                        AdminEmail = query.User.Institution.AdminEmail,
                        HostName = query.User.Institution.HostName,
                        Type = query.User.Institution.InstitutionType,
                        Logo = string.IsNullOrEmpty(query.User.Institution.Logo) ? "n/a" : query.User.Institution.Logo,
                        SenderEmail = query.User.Institution.SenderEmail,
                        SsoURL = string.IsNullOrEmpty(query.User.Institution.SsoURL) ? "n/a" : query.User.Institution.SsoURL,
                        SsoLogo = string.IsNullOrEmpty(query.User.Institution.SsoLogo) ? "n/a" : query.User.Institution.SsoLogo,
                        SSORequired = query.User.Institution.SSORequired,
                        DefaultLanguage = query.User.Institution.DefaultLanguage,
                        PrimaryThemeColor = query.User.Institution.PrimaryThemeColor,
                        SecondaryThemeColor = query.User.Institution.SecondaryThemeColor,
                        Status = (query.User.Institution.IsActive) ? "Active" : "Inactive",
                        DateCreated = query.User.Institution.CreatedAt
                    }
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<CandidateModel>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<CandidateModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<CandidateModel>>> GetAllCandidates(CandidateFilterModel search)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<CandidateModel>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Candidate> records = _context.Candidates
                                                .Include(x => x.User).ThenInclude(x => x.Institution)
                                                .Include(x => x.Faculty)
                                                .Include(x => x.Level)
                                                .Include(x => x.Department)
                                                .Include(x => x.Program)
                                                .Include(x => x.Semester)
                                                .Include(x => x.Session)
                                                .Where(x => x.InstitutionId == InsTId.Data);

                if (search.IsActive.HasValue)
                {
                    records = records.Where(x => x.IsActive == search.IsActive.Value);
                }
                if (search.LevelId.HasValue)
                {

                    records = records.Where(x => x.LevelId == search.LevelId.Value);
                }
                if (search.SessionId.HasValue)
                {

                    records = records.Where(x => x.SessionId == search.SessionId.Value);
                }

                if (search.SemesterId.HasValue)
                {

                    records = records.Where(x => x.SemesterId == search.SemesterId.Value);
                }

                if (search.DepartmentId.HasValue)
                {

                    records = records.Where(x => x.DepartmentId == search.DepartmentId.Value);
                }
                if (search.ProgramId.HasValue)
                {

                    records = records.Where(x => x.ProgramId == search.ProgramId.Value);
                }


                if (search.FacultyId.HasValue)
                {

                    records = records.Where(x => x.FacultyId == search.FacultyId.Value);
                }


                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.User.FirstName.ToLower().Contains(searchWord) ||
                        x.User.LastName.Contains(searchWord) ||
                        x.User.Email.ToLower().Contains(searchWord) ||
                        x.MatricNumber.ToLower().Contains(searchWord) ||
                        x.PhoneNumber.ToLower().Contains(searchWord) ||
                        x.Gender.ToLower().Contains(searchWord) ||
                        x.RegistrationNumber.ToLower().Contains(searchWord));
                }
                if (!string.IsNullOrEmpty(search.Name))
                {
                    records = records.Where(x => x.User.FirstName.ToLower().Contains(search.Name.ToLower()) ||
                                x.User.LastName.ToLower().Contains(search.Name.ToLower()));
                }
                if (!string.IsNullOrEmpty(search.RegNo))
                {
                    records = records.Where(x => x.RegistrationNumber.ToLower() == search.RegNo.ToLower());
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= search.StartDate && x.CreatedAt <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.User.FirstName);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }
                var result = await records.Select(query => new CandidateModel
                {
                    Id = query.Id,
                    FirstName = query.User.FirstName,
                    LastName = query.User.LastName,
                    Email = query.User.Email,
                    PhoneNumber = query.PhoneNumber,
                    DateOfBirth = query.DateOfBirth.Value,
                    CreatedAt = query.CreatedAt,
                    IsActive = query.IsActive,
                    Gender = query.Gender,
                    MatricNumber = query.MatricNumber,
                    PicturePath = string.IsNullOrEmpty(query.PicturePath) ? "" : query.PicturePath,
                    Level = query.Level.LevelName,
                    Semester = query.Semester == null ? null : new SemesterModel
                    {
                        Id = query.Semester.Id,
                        Name = query.Semester.SemesterName,
                        DateCreated = query.Semester.CreatedAt
                    },
                    Session = query.Session == null ? null : new SessionDto
                    {
                        Id = query.Session.Id,
                        Name = query.Session.Name,
                        DateCreated = query.Session.CreatedAt
                    },
                    Department = query.Department == null ? null : new DepartmentDto
                    {
                        Id = query.Department.Id,
                        Name = query.Department.Name,
                        DateCreated = query.Department.CreatedAt
                    },
                    Faculty = query.Faculty == null ? null : new FacultyDto
                    {
                        Id = query.Faculty.Id,
                        Name = query.Faculty.Name,
                        DateCreated = query.Faculty.CreatedAt
                    },
                    Program = query.Program == null ? null : new ProgramModel
                    {
                        Id = query.Program.Id,
                        Name = query.Program.Name,
                        DateCreated = query.Program.CreatedAt
                    },
                    Institution = query.User.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = query.User.Institution.Id,
                        Name = query.User.Institution.Name,
                        Code = query.User.Institution.Code,
                        AdminName = query.User.Institution.AdminName,
                        AdminEmail = query.User.Institution.AdminEmail,
                        HostName = query.User.Institution.HostName,
                        Type = query.User.Institution.InstitutionType,
                        Logo = string.IsNullOrEmpty(query.User.Institution.Logo) ? "n/a" : query.User.Institution.Logo,
                        SenderEmail = query.User.Institution.SenderEmail,
                        SsoURL = string.IsNullOrEmpty(query.User.Institution.SsoURL) ? "n/a" : query.User.Institution.SsoURL,
                        SsoLogo = string.IsNullOrEmpty(query.User.Institution.SsoLogo) ? "n/a" : query.User.Institution.SsoLogo,
                        SSORequired = query.User.Institution.SSORequired,
                        DefaultLanguage = query.User.Institution.DefaultLanguage,
                        PrimaryThemeColor = query.User.Institution.PrimaryThemeColor,
                        SecondaryThemeColor = query.User.Institution.SecondaryThemeColor,
                        Status = (query.User.Institution.IsActive) ? "Active" : "Inactive",
                        DateCreated = query.User.Institution.CreatedAt
                    }
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<CandidateModel>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<CandidateModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<CandidateModel>> GetById(long id)
        {
            try
            {
                var query = await _context.Candidates
                                .Include(x => x.User).ThenInclude(x => x.Institution)
                                .Include(x => x.Faculty)
                                .Include(x => x.Level)
                                .Include(x => x.Department)
                                .Include(x => x.Program)
                                .Include(x => x.Session)
                                .Include(x => x.Semester)
                                .Include(x => x.Region)
                                .Include(x => x.Country)
                                .Include(x => x.City)
                                .Include(x => x.Country)
                .FirstOrDefaultAsync(x => x.Id == id);

                if (query is null)
                    return CustomResult<CandidateModel>.ErrorOccured(ResponseMessages.CandidateDetailsNotFound, ResponseCodes.NotFoundErrorCode);

                var result = new CandidateModel
                {
                    Id = query.Id,
                    FirstName = query.User.FirstName,
                    LastName = query.User.LastName,
                    Email = query.User.Email,
                    PhoneNumber = query.PhoneNumber,
                    DateOfBirth = query.DateOfBirth.Value,
                    CreatedAt = query.CreatedAt,
                    IsActive = query.IsActive,
                    Gender = query.Gender,
                    MatricNumber = query.MatricNumber,
                    PicturePath = string.IsNullOrEmpty(query.PicturePath) ? "" : query.PicturePath,
                    Level = query.Level.LevelName,
                    Semester = query.Semester == null ? null : new SemesterModel
                    {
                        Id = query.Semester.Id,
                        Name = query.Semester.SemesterName,
                        DateCreated = query.Semester.CreatedAt
                    },
                    Session = query.Session == null ? null : new SessionDto
                    {
                        Id = query.Session.Id,
                        Name = query.Session.Name,
                        DateCreated = query.Session.CreatedAt
                    },
                    Department = query.Department == null ? null : new DepartmentDto
                    {
                        Id = query.Department.Id,
                        Name = query.Department.Name,
                        DateCreated = query.Department.CreatedAt
                    },
                    Faculty = query.Faculty == null ? null : new FacultyDto
                    {
                        Id = query.Faculty.Id,
                        Name = query.Faculty.Name,
                        DateCreated = query.Faculty.CreatedAt
                    },
                    Program = query.Program == null ? null : new ProgramModel
                    {
                        Id = query.Program.Id,
                        Name = query.Program.Name,
                        DateCreated = query.Program.CreatedAt
                    },
                    Institution = query.User.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = query.User.Institution.Id,
                        Name = query.User.Institution.Name,
                        Code = query.User.Institution.Code,
                        AdminName = query.User.Institution.AdminName,
                        AdminEmail = query.User.Institution.AdminEmail,
                        HostName = query.User.Institution.HostName,
                        Type = query.User.Institution.InstitutionType,
                        Logo = string.IsNullOrEmpty(query.User.Institution.Logo) ? "n/a" : query.User.Institution.Logo,
                        SenderEmail = query.User.Institution.SenderEmail,
                        SsoURL = string.IsNullOrEmpty(query.User.Institution.SsoURL) ? "n/a" : query.User.Institution.SsoURL,
                        SsoLogo = string.IsNullOrEmpty(query.User.Institution.SsoLogo) ? "n/a" : query.User.Institution.SsoLogo,
                        SSORequired = query.User.Institution.SSORequired,
                        DefaultLanguage = query.User.Institution.DefaultLanguage,
                        PrimaryThemeColor = query.User.Institution.PrimaryThemeColor,
                        SecondaryThemeColor = query.User.Institution.SecondaryThemeColor,
                        Status = (query.User.Institution.IsActive) ? "Active" : "Inactive",
                        DateCreated = query.User.Institution.CreatedAt
                    },
                    Country = query.Country == null ? null : new CountryListDto
                    {
                        Id = query.Country.Id,
                        Name = query.Country.Name
                    },
                    City = query.City == null ? null : new CityListDto
                    {
                        Id = query.City.Id,
                        Name = query.City.Name,
                    },
                    State = query.Region == null ? null : new RegionListDto
                    {
                        Id = query.Region.Id,
                        Name = query.Region.Name,
                    },
                    Levels = query.Level == null ? null : new LevelModel
                    {
                        Id = query.Level.Id,
                        LevelName = query.Level.LevelName,
                        DateCreated = query.Level.CreatedAt
                    }

                };

                return CustomResult<CandidateModel>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<CandidateModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<CandidateModel>> GetCandidateDetailByUserId(string candidateuserid)
        {
            try
            {
                var query = await _context.Candidates
                                .Include(x => x.User).ThenInclude(x => x.Institution)
                                .Include(x => x.Faculty)
                                .Include(x => x.Level)
                                .Include(x => x.Department)
                                .Include(x => x.Program)
                                .Include(x => x.Session)
                                .Include(x => x.Semester)
                                .Include(x => x.Region)
                                .Include(x => x.Country)
                                .Include(x => x.City)
                                .Include(x => x.Country)
                .FirstOrDefaultAsync(x => x.UserId == candidateuserid);

                if (query is null)
                    return CustomResult<CandidateModel>.ErrorOccured(ResponseMessages.CandidateDetailsNotFound, ResponseCodes.NotFoundErrorCode);

                var result = new CandidateModel
                {
                    Id = query.Id,
                    FirstName = query.User.FirstName,
                    LastName = query.User.LastName,
                    Email = query.User.Email,
                    PhoneNumber = query.PhoneNumber,
                    DateOfBirth = query.DateOfBirth.Value,
                    IsActive = query.IsActive,
                    Gender = query.Gender,
                    MatricNumber = query.MatricNumber,
                    PicturePath = string.IsNullOrEmpty(query.PicturePath) ? "" : query.PicturePath,
                    Level = query.Level.LevelName,
                    Semester = query.Semester == null ? null : new SemesterModel
                    {
                        Id = query.Semester.Id,
                        Name = query.Semester.SemesterName,
                        DateCreated = query.Semester.CreatedAt
                    },
                    Session = query.Session == null ? null : new SessionDto
                    {
                        Id = query.Session.Id,
                        Name = query.Session.Name,
                        DateCreated = query.Session.CreatedAt
                    },
                    Department = query.Department == null ? null : new DepartmentDto
                    {
                        Id = query.Department.Id,
                        Name = query.Department.Name,
                        DateCreated = query.Department.CreatedAt
                    },
                    Faculty = query.Faculty == null ? null : new FacultyDto
                    {
                        Id = query.Faculty.Id,
                        Name = query.Faculty.Name,
                        DateCreated = query.Faculty.CreatedAt
                    },
                    Program = query.Program == null ? null : new ProgramModel
                    {
                        Id = query.Program.Id,
                        Name = query.Program.Name,
                        DateCreated = query.Program.CreatedAt
                    },
                    Institution = query.User.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = query.User.Institution.Id,
                        Name = query.User.Institution.Name,
                        Code = query.User.Institution.Code,
                        AdminName = query.User.Institution.AdminName,
                        AdminEmail = query.User.Institution.AdminEmail,
                        HostName = query.User.Institution.HostName,
                        Type = query.User.Institution.InstitutionType,
                        Logo = string.IsNullOrEmpty(query.User.Institution.Logo) ? "n/a" : query.User.Institution.Logo,
                        SenderEmail = query.User.Institution.SenderEmail,
                        SsoURL = string.IsNullOrEmpty(query.User.Institution.SsoURL) ? "n/a" : query.User.Institution.SsoURL,
                        SsoLogo = string.IsNullOrEmpty(query.User.Institution.SsoLogo) ? "n/a" : query.User.Institution.SsoLogo,
                        SSORequired = query.User.Institution.SSORequired,
                        DefaultLanguage = query.User.Institution.DefaultLanguage,
                        PrimaryThemeColor = query.User.Institution.PrimaryThemeColor,
                        SecondaryThemeColor = query.User.Institution.SecondaryThemeColor,
                        Status = (query.User.Institution.IsActive) ? "Active" : "Inactive",
                        DateCreated = query.User.Institution.CreatedAt
                    },
                    Country = query.Country == null ? null : new CountryListDto
                    {
                        Id = query.Country.Id,
                        Name = query.Country.Name
                    },
                    City = query.City == null ? null : new CityListDto
                    {
                        Id = query.City.Id,
                        Name = query.City.Name,
                    },
                    State = query.Region == null ? null : new RegionListDto
                    {
                        Id = query.Region.Id,
                        Name = query.Region.Name,
                    },
                    Levels = query.Level == null ? null : new LevelModel
                    {
                        Id = query.Level.Id,
                        LevelName = query.Level.LevelName,
                        DateCreated = query.Level.CreatedAt
                    }

                };

                return CustomResult<CandidateModel>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<CandidateModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<CandidatePreviewModel>> CandidateProfilePreview(string email)
        {
            try
            {
                var tenantResult = await _userManagementService.GetTenantId();
                if (!tenantResult.IsSuccess)
                {
                    return CustomResult<CandidatePreviewModel>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.InstitutionId == tenantResult.Data);
                if(user == null)
                {
                    return CustomResult<CandidatePreviewModel>.ErrorOccured(ResponseMessages.CandidateDetailsNotFound, ResponseCodes.NotFoundErrorCode);
                }

                bool isCandidate = false;

                // Check if user has candidate role
                var userRoleIds = await _context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                if (userRoleIds.Any())
                {
                    isCandidate = await _context.Roles
                        .AnyAsync(r => userRoleIds.Contains(r.Id) && r.Name.ToLower() == "candidate");
                }

                var candidate = await _context.Candidates
                                .Include(x => x.Institution)
                                .Include(x => x.User)
                                .FirstOrDefaultAsync(x => x.UserId == user.Id && x.InstitutionId == tenantResult.Data);

                if (candidate == null)
                {
                    return CustomResult<CandidatePreviewModel>.ErrorOccured(ResponseMessages.CandidateDetailsNotFound, ResponseCodes.NotFoundErrorCode);
                }

                bool isFirstTimeLoginUser = true;

                if (candidate.DateOfBirth.HasValue &&
                    !string.IsNullOrEmpty(candidate.PicturePath) &&
                    !string.IsNullOrEmpty(candidate.Gender))
                {
                    isFirstTimeLoginUser = false;
                }

                var result = new CandidatePreviewModel
                {
                    Id = candidate.Id,
                    FirstName = candidate.User.FirstName,
                    LastName = candidate.User.LastName,
                    Email = candidate.User.Email,
                    PhoneNumber = candidate.PhoneNumber,
                    DateOfBirth = candidate.DateOfBirth.Value,
                    Gender = candidate.Gender,
                    MatricNumber = candidate.MatricNumber,
                    PicturePath = string.IsNullOrEmpty(candidate.PicturePath) ? "n/a" : candidate.PicturePath,
                    IsCandidate = isCandidate,
                    IsFirstTimeLoginUser = isFirstTimeLoginUser,
                    InstitutionId = candidate.InstitutionId.Value,
                    InstitutionName = candidate.Institution != null ? candidate.Institution.Name : "n/a"
                };

                return CustomResult<CandidatePreviewModel>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<CandidatePreviewModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public Task<CustomResult<List<List<string>>>> GetDownloadFormat()
        {
            var csvData = new List<List<string>>();

            // Headers
            csvData.Add(new List<string>
            {
                "FirstName", "LastName", "Email", "MatricNumber", "RegistrationNumber",
                "Gender", "PhoneNumber", "DateOfBirth", "Program", "Faculty", "Department"
            });

            // Dummy Records
            csvData.Add(new List<string>
            {
                "John", "Doe", "john@example.com", "MAT001", "REG001",
                "Male", "08012345678", "2000-01-01", "Computer Science", "Science", "Computer Science"
            });
            csvData.Add(new List<string>
            {
                "Jane", "Smith", "jane@example.com", "MAT002", "REG002",
                "Female", "08087654321", "1999-05-15", "Information Technology", "Science", "IT"
            });
            csvData.Add(new List<string>
            {
                "Mike", "Johnson", "mike@example.com", "MAT003", "REG003",
                "Male", "08099887766", "1998-03-10", "Engineering", "Engineering", "Mechanical"
            });
            csvData.Add(new List<string>
            {
                "Lucy", "Brown", "lucy@example.com", "MAT004", "REG004",
                "Female", "08077665544", "2001-07-20", "Business Admin", "Management", "Business"
            });
            csvData.Add(new List<string>
            {
                "Sam", "Williams", "sam@example.com", "MAT005", "REG005",
                "Male", "08033445566", "2002-11-05", "Economics", "Social Sciences", "Economics"
            });

            return Task.FromResult(CustomResult<List<List<string>>>.Success(csvData));
        }

        public async Task<CustomResult<List<MigrationErrorVM>>> BulkUploadCandidatesAsync(CandidateUploadModel uploadModel)
        {
            var supportedTypes = new[] { "csv" };
            var errorList = new List<MigrationErrorVM>();

            try
            {
                if (uploadModel.File == null || uploadModel.File.Length == 0)
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("No file uploaded.", ResponseCodes.BadRequestErrorCode);

                var tenantIdResult = await _userManagementService.GetTenantId();
                if (!tenantIdResult.IsSuccess)
                    return CustomResult<List<MigrationErrorVM>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                var getInstitution = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == tenantIdResult.Data);

                if (getInstitution is null) return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Institution does not exists", ResponseCodes.NotFoundErrorCode);

                var fileExt = Path.GetExtension(uploadModel.File.FileName).Substring(1);
                if (!supportedTypes.Contains(fileExt))
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Only .csv file extension is allowed!", ResponseCodes.BadRequestErrorCode);

                var tempFilename = Guid.NewGuid().ToString("N") + Path.GetExtension(uploadModel.File.FileName);
                var path = Path.Combine(Path.GetTempPath(), tempFilename);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await uploadModel.File.CopyToAsync(stream);
                }

                var allLines = File.ReadAllLines(path).ToList();

                if (allLines.Count > 100)
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Rows should not be more than 100. Kindly reduce the records into multiple sheets.", ResponseCodes.BadRequestErrorCode);

                if (allLines.Count <= 1)
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("CSV must contain a header and at least one data row.", ResponseCodes.BadRequestErrorCode);

                var headerValidation = ValidateCandidateCSVHeader(allLines[0]);
                if (!headerValidation.IsSuccess)
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured(headerValidation.Message, ResponseCodes.BadRequestErrorCode);

                var level = await _context.Levels.FirstOrDefaultAsync(x => x.Id == uploadModel.LevelId);
                if (level == null)
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Invalid Level passed.", ResponseCodes.BadRequestErrorCode);

                var session = await _context.Sessions.FirstOrDefaultAsync(x => x.Id == uploadModel.SessionId);
                if (session == null)
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Invalid session passed.", ResponseCodes.BadRequestErrorCode);

                var semester = await _context.Semesters.FirstOrDefaultAsync(x => x.Id == uploadModel.SemesterId);
                if (semester == null)
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Invalid semester passed.", ResponseCodes.BadRequestErrorCode);

                int savedItemCount = 0;
                var buildEmailModel = new List<SendWelcomeEmailVM>();
                var candidateRoleId = _authService.GetRoleIdbyRoleName(RolesEnum.CANDIDATE.GetEnumText()).Result;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    for (int i = 1; i < allLines.Count; i++)
                    {
                        var row = allLines[i];
                        var columns = row.Split(',');

                        try
                        {
                            var firstName = columns[0].Trim();
                            var lastName = columns[1].Trim();
                            var email = columns[2].Trim();
                            var matricNumber = columns[3].Trim();
                            var registrationNumber = columns[4].Trim();
                            var gender = columns[5].Trim();
                            var phoneNumber = columns[6].Trim();
                            var dobStr = columns[7].Trim();
                            var programStr = columns[8].Trim();
                            var facultyStr = columns[9].Trim();
                            var departmentStr = columns[10].Trim();

                            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
                            {
                                errorList.Add(new MigrationErrorVM
                                {
                                    RowNum = i + 1,
                                    RecordIdentifier = email,
                                    ErrorMessage = "Missing mandatory fields (FirstName, LastName, Email)."
                                });
                                continue;
                            }

                            var isMatricNumberExist = await _context.Candidates.AnyAsync(x => x.MatricNumber.ToLower() == matricNumber.ToLower()
                                    && x.InstitutionId == tenantIdResult.Data);
                            if (isMatricNumberExist)
                            {
                                errorList.Add(new MigrationErrorVM
                                {
                                    RowNum = i + 1,
                                    RecordIdentifier = email,
                                    ErrorMessage = $"Matric number - {matricNumber} already exist for another student"
                                });
                                continue;
                            }

                            var department = await _context.Departments.FirstOrDefaultAsync(x => x.Name.ToLower() == departmentStr.ToLower() && x.InstitutionId == tenantIdResult.Data);
                            if (department is null)
                            {
                                errorList.Add(new MigrationErrorVM
                                {
                                    RowNum = i + 1,
                                    RecordIdentifier = departmentStr,
                                    ErrorMessage = $"Department - {departmentStr} not found on our system."
                                });
                                continue;
                            }
                            var faculty = await _context.Faculties.FirstOrDefaultAsync(x => x.Name.ToLower() == facultyStr.ToLower() && x.InstitutionId == tenantIdResult.Data);
                            if (faculty is null)
                            {
                                errorList.Add(new MigrationErrorVM
                                {
                                    RowNum = i + 1,
                                    RecordIdentifier = facultyStr,
                                    ErrorMessage = $"Faculty - {facultyStr} not found on our system."
                                });
                                continue;
                            }
                            var program = await _context.Programs.FirstOrDefaultAsync(x => x.Name.ToLower() == programStr.ToLower() && x.InstitutionId == tenantIdResult.Data);
                            if (program is null)
                            {
                                errorList.Add(new MigrationErrorVM
                                {
                                    RowNum = i + 1,
                                    RecordIdentifier = programStr,
                                    ErrorMessage = $"Program - {programStr} not found on our system."
                                });
                                continue;
                            }

                            if (!DateTime.TryParse(dobStr, out DateTime dateOfBirth))
                            {
                                errorList.Add(new MigrationErrorVM
                                {
                                    RowNum = i + 1,
                                    RecordIdentifier = email,
                                    ErrorMessage = "Invalid DateOfBirth format."
                                });
                                continue;
                            }

                            var existingUser = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower() && x.InstitutionId == tenantIdResult.Data);
                            if (existingUser != null)
                            {
                                errorList.Add(new MigrationErrorVM
                                {
                                    RowNum = i + 1,
                                    RecordIdentifier = email,
                                    ErrorMessage = $"Email '{email}' already exists."
                                });
                                continue;
                            }

                            var user = new ApplicationUser
                            {
                                Id = Guid.NewGuid().ToString(),
                                FirstName = firstName,
                                LastName = lastName,
                                Email = email,
                                UserName = email,
                                InstitutionId = tenantIdResult.Data,
                                IsActive = false,
                                IsDeleted = false,
                                CreatedAt = DateTime.UtcNow,
                                RequiresPasswordChange = true
                            };

                            var password = lastName.Replace(" ", "").Trim() + "@123";
                            var createResult = await _userManager.CreateAsync(user, password);
                            if (!createResult.Succeeded)
                            {
                                errorList.Add(new MigrationErrorVM
                                {
                                    RowNum = i + 1,
                                    RecordIdentifier = email,
                                    ErrorMessage = $"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}"
                                });
                                continue;
                            }
                            //Log default password 
                            _logger.LogInformation($"new User to create with email: {email}, Default Password: {password}");
                            var addUserToRole = await _context.UserRoles.AddAsync(new ApplicationUserRole
                            {
                                UserId = user.Id,
                                RoleId = candidateRoleId,
                                InstitutionId = tenantIdResult.Data
                            });

                            var candidate = new Candidate
                            {
                                UserId = user.Id,
                                InstitutionId = tenantIdResult.Data,
                                DepartmentId = department.Id,
                                FacultyId = faculty.Id,
                                ProgramId = program.Id,
                                LevelId = level.Id,
                                SemesterId = semester.Id,
                                SessionId = session.Id,
                                MatricNumber = matricNumber,
                                RegistrationNumber = registrationNumber,
                                Gender = gender,
                                IsActive = true,
                                IsDeleted = false,
                                PhoneNumber = phoneNumber,
                                DateOfBirth = dateOfBirth,
                                CityId = null,
                                RegionId = null,
                                CountryId = null,
                                CreatedAt = DateTime.UtcNow
                            };

                            var studentState = new CandidatesCurrentState
                            {
                                Candidate = candidate,
                                CreatedAt = DateTime.UtcNow,
                                LevelId = level.Id,
                                SemesterId = semester.Id,
                                SessionId = session.Id,
                                IsActive = true,
                                UpdatedAt = DateTime.UtcNow,
                                IsDeleted = false,
                                IsCurrent = true,
                                InstitutionId = tenantIdResult.Data
                            };

                            await _context.Candidates.AddAsync(candidate);
                            await _context.CandidatesCurrentStates.AddAsync(studentState);

                            buildEmailModel.Add(new SendWelcomeEmailVM
                            {
                                Fullname = $"{lastName} {firstName}",
                                InstitutionBaseUrl = $"{getInstitution.HostName}/auth/login",
                                receiverEmail = email,
                                Password = password
                            });

                            savedItemCount++;
                        }
                        catch (Exception ex)
                        {
                            errorList.Add(new MigrationErrorVM
                            {
                                RowNum = i + 1,
                                ErrorMessage = "Unexpected error while processing row.",
                                AdditionalMessage = ex.Message
                            });
                        }
                    }
                    await _context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception(ex.Message);
                }

                // send mail using background service
                if (buildEmailModel.Count > 0)
                {
                    foreach (var model in buildEmailModel)
                    {
                        _backgroundEmailQueue.QueueEmail(model);
                    }
                }
                if (errorList.Count > 0)
                {
                    _logger.LogWarning("Bulk upload completed with errors. Check the error list for details.");
                    return CustomResult<List<MigrationErrorVM>>.Success(errorList, $"An error occurred while trying to upload candidate but {savedItemCount} uploaded");
                }
                else
                {
                    _logger.LogInformation("Bulk upload completed successfully.");
                    return CustomResult<List<MigrationErrorVM>>.Success(errorList, $"{savedItemCount} uploaded successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<List<MigrationErrorVM>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        private CustomResult<string> ValidateCandidateCSVHeader(string headerLine)
        {
            var expectedHeaders = new[]
            {
                "FirstName", "LastName", "Email", "MatricNumber", "RegistrationNumber", "Gender", "PhoneNumber", "DateOfBirth", "Program", "Faculty", "Department"
            };

            var actualHeaders = headerLine
                                .Trim('\uFEFF')
                                .Split(',')
                                .Select(h => h.Trim().Trim('"'))
                                .Where(h => !string.IsNullOrWhiteSpace(h))
                                .ToArray();

            if (!expectedHeaders.SequenceEqual(actualHeaders, StringComparer.OrdinalIgnoreCase))
            {
                return CustomResult<string>.ErrorOccured("CSV header format is not valid. Expected headers: " +
                                                         string.Join(", ", expectedHeaders), ResponseCodes.BadRequestErrorCode);
            }
            return CustomResult<string>.Success("Header valid.");
        }

        public async Task<CustomResult<ExportStudentExamsDto>> ExportCandidates(CandidateExportModel search)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<ExportStudentExamsDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);

                IQueryable<Candidate> records = _context.Candidates
                    .Include(x => x.Department)
                    .Include(x => x.Program)
                    .Include(x => x.Faculty)
                    .Include(x => x.Semester)
                    .Include(x => x.Session)
                    .Include(x => x.User)
                    .Include(x => x.Country)
                    .Include(x => x.Region)
                    .Include(x => x.City)
                    .Where(x => x.InstitutionId == InsTId.Data)
                    .OrderByDescending(x => x.Id);

                if (search.DepartmentId > 0)
                {
                    records = records.Where(x => x.DepartmentId == search.DepartmentId).AsQueryable();
                }
                if (search.ProgramId > 0)
                {
                    records = records.Where(x => x.ProgramId == search.ProgramId).AsQueryable();
                }
                if (search.FacultyId > 0)
                {
                    records = records.Where(x => x.FacultyId == search.FacultyId).AsQueryable();
                }
                if (search.LevelId > 0)
                {
                    records = records.Where(x => x.LevelId == search.LevelId).AsQueryable();
                }
                if (search.SessionId > 0)
                {
                    records = records.Where(x => x.SessionId == search.SessionId).AsQueryable();
                }
                if (search.SemesterId > 0)
                {
                    records = records.Where(x => x.SemesterId == search.SemesterId).AsQueryable();
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= search.StartDate && x.CreatedAt <= endDate);
                }

                var result = records.Select(x => new CandidateExportListModel
                {
                    MatricNumber = x.MatricNumber,
                    Gender = x.Gender,
                    PhoneNumber = x.PhoneNumber,
                    DateOfBirth = x.DateOfBirth,
                    CandidateName = x.User == null ? "n/a" : x.User.LastName + " " + x.User.FirstName,
                    Department = x.Department == null ? "n/a" : x.Department.Name,
                    Faculty = x.Faculty == null ? "n/a" : x.Faculty.Name,
                    Program = x.Program == null ? "n/a" : x.Program.Name,
                    Level = x.Level == null ? "n/a" : x.Level.LevelName,
                    Session = x.Session == null ? "n/a" : x.Session.Name,
                    Semester = x.Semester == null ? "n/a" : x.Semester.SemesterName,
                    Institution = x.Institution == null ? "n/a" : x.Institution.Name,
                    Country = x.Country == null ? "n/a" : x.Country.Name,
                    State = x.Region == null ? "n/a" : x.Region.Name,
                    City = x.City == null ? "n/a" : x.City.Name,
                    Timestamp = x.CreatedAt.ToString("dd MMM yyyy HH:mm tt")
                }).AsQueryable();

                if (!result.Any())
                {
                    return CustomResult<ExportStudentExamsDto>.ErrorOccured("No record found", ResponseCodes.NotFoundErrorCode);
                }

                var sb = new StringBuilder();
                sb.AppendLine("MatricNumber,CandidateName,Faculty,Department,Program,Level,Session,Semester,Institution,PhoneNumber,Gender,DateOfBirth,Country, State, City");

                foreach (var item in result)
                {
                    sb.AppendLine($"{item.MatricNumber},{item.CandidateName},{item.Faculty},{item.Department},{item.Program},{item.Level},{item.Session},{item.Semester},{item.Institution},{item.PhoneNumber},{item.Gender},{item.DateOfBirth},{item.Country},{item.State},{item.City}");
                }

                var csvString = sb.ToString();
                var csvBytes = Encoding.UTF8.GetBytes(csvString);
                var base64Csv = Convert.ToBase64String(csvBytes);

                var exportDto = new ExportStudentExamsDto
                {
                    Base64File = base64Csv,
                    FileName = $"CandidateExport_{DateTime.Now:ddMMyyyyHHmmss}.csv",
                    ContentType = "text/csv"
                };

                return CustomResult<ExportStudentExamsDto>.Success(exportDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ExportStudentExamsDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}