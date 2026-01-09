using AutoMapper;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using LS1.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using edutech.services.examportal.Data.DbContext;
using edutech.services.examportal.Core.Interfaces;
using Microsoft.Extensions.Logging;
using examportal.Data.Entities;
using SISService.BoilerPlate.Service.Interfaces;
using edutech.services.examportal.Data.DTOs.Results;
using edutech.services.examportal.Data.DTOs.ResponseDto;

namespace edutech.services.examportal.Core.Services;

public class ApplicationUserService : IApplicationUserService
{
    private readonly IMapper _mapper;
    private readonly IConfiguration _config;
    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<ApplicationUserService> _logger;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IUtilityService _utilityService;


    public ApplicationUserService(ILogger<ApplicationUserService> logger, IAuthService authService,
        IMemoryCache memoryCache, IUtilityService utilityService,
        RoleManager<ApplicationRole> roleManager, ITokenService tokenService,
        UserManager<ApplicationUser> userManager, IMapper mapper, IHttpClientService httpClientService,
        AppDbContext dbContext, IConfiguration config, IEmailService emailService)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _authService = authService;
        _httpClientService = httpClientService;
        _config = config;
        _dbContext = dbContext;
        _mapper = mapper; _tokenService = tokenService;
        _roleManager = roleManager;
        _userManager = userManager;
        _emailService = emailService;
        _utilityService = utilityService;
    }

    public async Task<CustomResult<ApplicationUserSignUpResponse>> InviteAsync1(ApplicationUserInviteRequest applicationUserInviteRequest, DateTime currentDateTime, string currentUser)
    {
        try
        {
            // Validate user
            var existingUser = await GetUserByEmailAsync(applicationUserInviteRequest.Email);

            if (existingUser != null)
            {
                _logger.LogError("User with email address {email} already exist", applicationUserInviteRequest.Email);
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.AccountAlreadyExistsError, ResponseCodes.AccountAlreadyExistErrorCode);
            }

            // Validate organization
            var org = await _dbContext.Organizations
                .FirstOrDefaultAsync(x => x.Id == applicationUserInviteRequest.OrganizationId);

            if (org == null)
            {
                _logger.LogError("Organization with id of {id} does not exist", applicationUserInviteRequest.OrganizationId);
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.OrganizationNotExisting, ResponseCodes.OrganizationNotExisting);
            }

            // Validate role
            var role = await _roleManager.Roles
                .FirstOrDefaultAsync(r => r.Id == applicationUserInviteRequest.RoleId);

            if (role == null)
            {
                _logger.LogError("Role with id of {roleId} does not exist", applicationUserInviteRequest.RoleId);
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.UserRoleNotFoundError, ResponseCodes.RoleNotFoundErrorCode);
            }

            var transporterRole = await _dbContext.Roles
             .Where(r => r.Name == "TRANSPORTER")
             .Select(r => r.Id)
             .FirstOrDefaultAsync();

            if (applicationUserInviteRequest.RoleId == transporterRole)
                //Validate that applicationUserInviteRequest has name, telephone and address
                if (string.IsNullOrEmpty(applicationUserInviteRequest.Name) || string.IsNullOrEmpty(applicationUserInviteRequest.Telephone) || applicationUserInviteRequest.Address == null)
                {
                    return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.TransporterDetailsRequired, ResponseCodes.TransporterDetailsRequired);
                }


            // Generate random password and create Firebase profile
            var randomPassword = Utility.GenerateOTP(6);
            var profileOnFA = await _authService.ProfileFireBaseUser(applicationUserInviteRequest.Email, randomPassword);

            if (!profileOnFA.IsProfiled)
            {
                await _authService.DeleteFireBaseUserNyEmail(applicationUserInviteRequest.Email);
                return CustomResult<ApplicationUserSignUpResponse>.Failure(new CustomError(profileOnFA.Message), ResponseCodes.OperationError);
            }

            // Map request data to user entity
            var user = _mapper.Map<ApplicationUser>(applicationUserInviteRequest);
            user.FirebaseId = profileOnFA.UserId;

            // Generate reset token
            var resetToken = await _tokenService.generateResetTokenAsync();

            // Populate user fields
            user.UserName = user.Email = applicationUserInviteRequest.Email;
            user.NormalizedEmail = user.NormalizedUserName = applicationUserInviteRequest.Email;
            user.RoleId = applicationUserInviteRequest.RoleId;
            user.IsActive = true;
            user.DateCreated = DateTime.UtcNow;
            user.EmailVerificationToken = resetToken;
            user.ApprovalStatus = ApprovalStatuses.Approved;
            user.MustChangePassword = true;
            user.Organization = org;

            // Create user in system
            var result = await _userManager.CreateAsync(user, "_Otp-" + randomPassword);

            if (result == null || !result.Succeeded)
            {
                await _authService.DeleteFireBaseUserNyEmail(applicationUserInviteRequest.Email);
                var error = result!.Errors.Select(e => e.Description).FirstOrDefault();
                return CustomResult<ApplicationUserSignUpResponse>.Failure(new CustomError(error!), ResponseCodes.UserInvitationFailedErrorCode);
            }

            // Assign role claim
            await _authService.AssignRoleClaimAsync(user.FirebaseId!, role.Name);

            // Add user to role if not already in the role
            if (!await _userManager.IsInRoleAsync(user, role.Name))
            {
                await _userManager.AddToRoleAsync(user, role.Name);
            }

            // Map the user data for response
            var mappedData = _mapper.Map<ApplicationUserSignUpResponse>(user);

            // Prepare and send email invitation
            _ = Task.Run(async () =>
            {
                var mailBody = EmailTemplate.OTPEmail;
                mailBody = mailBody.Replace("{{OrgEMail}}", "info@thels1.com")
                    .Replace("{{OTP1}}", randomPassword[0].ToString())
                    .Replace("{{OTP2}}", randomPassword[1].ToString())
                    .Replace("{{OTP3}}", randomPassword[2].ToString())
                    .Replace("{{OTP4}}", randomPassword[3].ToString())
                    .Replace("{{OTP5}}", randomPassword[4].ToString())
                    .Replace("{{OTP6}}", randomPassword[5].ToString());

                var mailSubject = $"{EmailTemplate.OTPEmailSubject} - {randomPassword}";
                var sendEmailRequest = new SendEmailRequestDto
                {
                    EmailBody = mailBody,
                    Subject = mailSubject,
                    RecipientEmail = user.Email,
                    RecipientName = user.FullName
                };

                await _emailService.SendMailAsync(sendEmailRequest);
            });
            //Add Transporter and Driver objects
            if (applicationUserInviteRequest.RoleId == transporterRole)
            {
                var transporter = new Transporter
                {
                    UserId = user.Id,
                    TransporterName = applicationUserInviteRequest.Name,
                    Telephone = applicationUserInviteRequest.Telephone,
                    Address = applicationUserInviteRequest.Address,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _dbContext.Transporters.AddAsync(transporter);
                await _dbContext.SaveChangesAsync();
            }
            return CustomResult<ApplicationUserSignUpResponse>.Success(mappedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(ApplicationUserService).Name, nameof(InviteAsync));
            return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
        }
    }
    public async Task<CustomResult<ApplicationUserSignUpResponse>> InviteAsync(ApplicationUserInviteRequest applicationUserInviteRequest, DateTime currentDateTime, string currentUser)
    {
        try
        {
            // Validate user
            if (await GetUserByEmailAsync(applicationUserInviteRequest.Email) != null)
            {
                _logger.LogWarning("User with email {Email} already exists", applicationUserInviteRequest.Email);
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.AccountAlreadyExistsError, ResponseCodes.AccountAlreadyExistErrorCode);
            }

            // Validate organization and role in a single trip to the database
            var orgAndRole = await (from o in _dbContext.Organizations
                                    join r in _roleManager.Roles on 1 equals 1
                                    where o.Id == applicationUserInviteRequest.OrganizationId &&
                                          r.Id == applicationUserInviteRequest.RoleId
                                    select new { Org = o, Role = r }).FirstOrDefaultAsync();

            if (orgAndRole?.Org == null)
            {
                _logger.LogWarning("Organization with id {OrganizationId} not found", applicationUserInviteRequest.OrganizationId);
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.OrganizationNotExisting, ResponseCodes.OrganizationNotExisting);
            }

            if (orgAndRole?.Role == null)
            {
                _logger.LogWarning("Role with id {RoleId} not found", applicationUserInviteRequest.RoleId);
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.UserRoleNotFoundError, ResponseCodes.RoleNotFoundErrorCode);
            }

            // Load transporter, driver, and customer role IDs in one query
            var roleIds = await _dbContext.Roles.Where(r => r.Name == "TRANSPORTER" || r.Name == "DRIVER" || r.Name == "CUSTOMER").ToDictionaryAsync(r => r.Name, r => r.Id);

            var transporterRole = roleIds.GetValueOrDefault("TRANSPORTER");
            var customerRole = roleIds.GetValueOrDefault("CUSTOMER");

            // Validate required details
            if (string.IsNullOrWhiteSpace(applicationUserInviteRequest.Name) || string.IsNullOrWhiteSpace(applicationUserInviteRequest.Telephone) || applicationUserInviteRequest.Address == null)
            {
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.TransporterDetailsRequired, ResponseCodes.TransporterDetailsRequired);
            }

            // Generate password and create Firebase profile
            var randomPassword = Utility.GenerateOTP(6);
            var profileOnFA = await _authService.ProfileFireBaseUser(applicationUserInviteRequest.Email, randomPassword);

            if (!profileOnFA.IsProfiled)
            {
                await _authService.DeleteFireBaseUserNyEmail(applicationUserInviteRequest.Email);
                return CustomResult<ApplicationUserSignUpResponse>.Failure(new CustomError(profileOnFA.Message), ResponseCodes.OperationError);
            }

            // Map user data
            var user = _mapper.Map<ApplicationUser>(applicationUserInviteRequest);
            user.PhoneNumber = applicationUserInviteRequest?.Telephone;
            user.FullName = applicationUserInviteRequest.Name;
            user.DisplayName = applicationUserInviteRequest.Name;
            user.Email = applicationUserInviteRequest.Email;
            user.FirebaseId = profileOnFA.UserId;
            user.UserName = user.Email = applicationUserInviteRequest.Email;
            user.NormalizedEmail = user.NormalizedUserName = applicationUserInviteRequest.Email.ToUpper();
            user.RoleId = applicationUserInviteRequest.RoleId;
            user.IsActive = true;
            user.DateCreated = DateTime.UtcNow;
            user.EmailVerificationToken = await _tokenService.generateResetTokenAsync();
            user.ApprovalStatus = ApprovalStatuses.Approved;
            user.MustChangePassword = true;
            user.Organization = orgAndRole.Org;

            // Create user
            var result = await _userManager.CreateAsync(user, "_Otp-" + randomPassword);

            if (result?.Succeeded != true)
            {
                await _authService.DeleteFireBaseUserNyEmail(applicationUserInviteRequest.Email);
                var error = result.Errors.FirstOrDefault()?.Description;
                return CustomResult<ApplicationUserSignUpResponse>.Failure(new CustomError(error ?? "Unknown error"), ResponseCodes.UserInvitationFailedErrorCode);
            }

            // Assign role and claim
            await _authService.AssignRoleClaimAsync(user.FirebaseId!, orgAndRole.Role.Name);
            if (!await _userManager.IsInRoleAsync(user, orgAndRole.Role.Name))
            {
                await _userManager.AddToRoleAsync(user, orgAndRole.Role.Name);
            }

            // Send email asynchronously using a background queue
            var mailBody = EmailTemplate.OTPEmail
                .Replace("{{OrgEMail}}", "info@thels1.com")
                .Replace("{{OTP1}}", randomPassword[0].ToString())
                .Replace("{{OTP2}}", randomPassword[1].ToString())
                .Replace("{{OTP3}}", randomPassword[2].ToString())
                .Replace("{{OTP4}}", randomPassword[3].ToString())
                .Replace("{{OTP5}}", randomPassword[4].ToString())
                .Replace("{{OTP6}}", randomPassword[5].ToString());

            var mailSubject = $"{EmailTemplate.OTPEmailSubject} - {randomPassword}";
            var sendEmailRequest = new SendEmailRequestDto
            {
                EmailBody = mailBody,
                Subject = mailSubject,
                RecipientEmail = user.Email,
                RecipientName = user.FullName
            };

            _ = _emailService.SendMailAsync(sendEmailRequest);

            // Add role-specific data
            if (applicationUserInviteRequest.RoleId == transporterRole)
            {
                var transporter = new Transporter
                {
                    UserId = user.Id,
                    TransporterName = applicationUserInviteRequest.Name,
                    Telephone = applicationUserInviteRequest.Telephone,
                    Address = applicationUserInviteRequest.Address,

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _dbContext.Transporters.AddAsync(transporter);
            }
            else if (applicationUserInviteRequest.RoleId == customerRole)
            {
                var customer = new Customer
                {
                    UserId = user.Id.ToString(),
                    CustomerName = applicationUserInviteRequest.Name,
                    CustomerTelephone = applicationUserInviteRequest.Telephone,
                    CustomerEmail = applicationUserInviteRequest.Email,
                    CustomerAddress = applicationUserInviteRequest.Address,
                    CompanyAddress = applicationUserInviteRequest.CompanyDetail?.CompanyAddress,
                    CompanyEmail = applicationUserInviteRequest.CompanyDetail?.CompanyEmail,
                    CompanyName = applicationUserInviteRequest.CompanyDetail.CompanyName,
                    CompanyTelephone = applicationUserInviteRequest.CompanyDetail?.CompanyTelephone,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _dbContext.Customers.AddAsync(customer);
            }

            await _dbContext.SaveChangesAsync();

            // Return success response
            var mappedData = _mapper.Map<ApplicationUserSignUpResponse>(user);
            return CustomResult<ApplicationUserSignUpResponse>.Success(mappedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}",
                nameof(ApplicationUserService), nameof(InviteAsync));
            return CustomResult<ApplicationUserSignUpResponse>.Failure(
                CustomError.SystemExceptionError,
                ResponseCodes.OperationError);
        }
    }

    public async Task<CustomResult<ApplicationUserSignUpResponse>> InviteAsync(UserInvitationRequestDto model)
    {
        try
        {
            // Validate user
            var existingUser = await GetUserByEmailAsync(model.Email);

            if (existingUser != null)
            {
                _logger.LogError("User with email address {email} already exist", model.Email);
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.AccountAlreadyExistsError, ResponseCodes.AccountAlreadyExistErrorCode);
            }

            var driverRoleId = await _dbContext.Roles
             .Where(r => r.Name == UserRole.Driver.GetEnumText())
             .AsNoTracking()
             .Select(r => r.Id)
             .FirstOrDefaultAsync();

            if (driverRoleId == Guid.Empty)
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.RoleNotFoundError, ResponseCodes.RoleNotFoundErrorCode);

            // Generate random password and create Firebase profile
            var randomPassword = Utility.GenerateOTP(6);

            var profileOnFA = await _authService.ProfileFireBaseUser(model.Email, randomPassword);

            if (!profileOnFA.IsProfiled)
            {
                await _authService.DeleteFireBaseUserNyEmail(model.Email);
                return CustomResult<ApplicationUserSignUpResponse>.Failure(new CustomError(profileOnFA.Message), ResponseCodes.OperationError);
            }

            // Generate reset token
            var resetToken = await _tokenService.generateResetTokenAsync();

            var user = new ApplicationUser()
            {
                RoleId = driverRoleId,
                DisplayName = model.Name,
                FullName = model.Name,
                Address = model.Address,
                FirebaseId = profileOnFA.UserId,
                IsActive = true,
                UserName = model.Email,
                NormalizedEmail = model.Email.ToUpper(),
                DateCreated = DateTime.UtcNow,
                EmailVerificationToken = resetToken,
                ApprovalStatus = ApprovalStatuses.Approved,
                MustChangePassword = true,
                Email = model.Email
            };

            // Create user in system
            var result = await _userManager.CreateAsync(user, "_Otp-" + randomPassword);

            if (result == null || !result.Succeeded)
            {
                await _authService.DeleteFireBaseUserNyEmail(model.Email);
                var error = result!.Errors.Select(e => e.Description).FirstOrDefault();
                return CustomResult<ApplicationUserSignUpResponse>.Failure(new CustomError(error!), ResponseCodes.UserInvitationFailedErrorCode);
            }

            // Assign role claim
            await _authService.AssignRoleClaimAsync(user.FirebaseId!, UserRole.Driver.GetEnumText());

            // Add user to role if not already in the role
            if (!await _userManager.IsInRoleAsync(user, UserRole.Driver.GetEnumText()))
                await _userManager.AddToRoleAsync(user, UserRole.Driver.GetEnumText());

            // Map the user data for response
            var mappedData = _mapper.Map<ApplicationUserSignUpResponse>(user);

            // Prepare and send email invitation
            _ = Task.Run(async () =>
            {
                var mailBody = EmailTemplate.OTPEmail;
                mailBody = mailBody.Replace("{{OrgEMail}}", "info@thels1.com")
                    .Replace("{{OTP1}}", randomPassword[0].ToString())
                    .Replace("{{OTP2}}", randomPassword[1].ToString())
                    .Replace("{{OTP3}}", randomPassword[2].ToString())
                    .Replace("{{OTP4}}", randomPassword[3].ToString())
                    .Replace("{{OTP5}}", randomPassword[4].ToString())
                    .Replace("{{OTP6}}", randomPassword[5].ToString());

                var mailSubject = $"{EmailTemplate.OTPEmailSubject} - {randomPassword}";
                var sendEmailRequest = new SendEmailRequestDto
                {
                    EmailBody = mailBody,
                    Subject = mailSubject,
                    RecipientEmail = user.Email,
                    RecipientName = user.FullName
                };

                await _emailService.SendMailAsync(sendEmailRequest);
            });

            return CustomResult<ApplicationUserSignUpResponse>.Success(mappedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(ApplicationUserService).Name, nameof(InviteAsync));
            return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
        }
    }

    public async Task<CustomResult<ApplicationUserSignUpResponse>> InviteAsyncV2(ApplicationUserInviteRequest applicationUserInviteRequest, DateTime currentDateTime, string currentUser)
    {
        try
        {
            var existingUser = await GetUserByEmailAsync(applicationUserInviteRequest.Email);

            if (existingUser != null)
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.AccountAlreadyExistsError, ResponseCodes.AccountAlreadyExistErrorCode);

            //pick the organization Id

            var org = await _dbContext.Organizations.FirstOrDefaultAsync(x => x.Id == applicationUserInviteRequest.OrganizationId);

            if (org == null)
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.OrganizationNotExisting, ResponseCodes.OrganizationNotExisting);

            var randomPassword = Utility.GenerateOTP(6);
            //var randomPassword = "Password@123";

            //Send the details to FireBase for Profiling

            //var profileOnFA = await _authService.SignUp(applicationUserInviteRequest.Email, randomPassword);
            var profileOnFA = await _authService.SignUpv2(applicationUserInviteRequest.Email, randomPassword);

            if (!profileOnFA.IsProfiled)
            {
                return CustomResult<ApplicationUserSignUpResponse>.Failure(new CustomError(profileOnFA.Message), ResponseCodes.OperationError);
            }

            if (string.IsNullOrEmpty(profileOnFA.UserId))
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.FireBaseSignUpFailed, ResponseCodes.FireBaseSignUpFailed);

            _logger.LogInformation($"User Email: {applicationUserInviteRequest.Email}, Successfully profiled on Firebase with UID: {profileOnFA.UserId}");

            var user = _mapper.Map<ApplicationUser>(applicationUserInviteRequest);
            user.FirebaseId = profileOnFA.UserId;
            string role = string.Empty;

            if (applicationUserInviteRequest.RoleId != Guid.Empty)
            {
                var roleResult = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == applicationUserInviteRequest.RoleId);

                if (roleResult == null)
                    return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.UserRoleNotFoundError, ResponseCodes.RoleNotFoundErrorCode);

                role = roleResult?.Name!;

                //assign role claim to the user
                await _authService.AssignRoleClaimAsync(user.FirebaseId, role);
            }


            var resetToken = await _tokenService.generateResetTokenAsync();

            user.UserName = applicationUserInviteRequest.Email;
            user.Email = applicationUserInviteRequest.Email;
            user.IsActive = true;
            user.NormalizedEmail = applicationUserInviteRequest.Email;
            user.NormalizedUserName = applicationUserInviteRequest.Email;
            user.RoleId = applicationUserInviteRequest.RoleId;
            user.Email = applicationUserInviteRequest.Email;
            user.IsActive = true;
            user.DateCreated = DateTime.UtcNow;
            user.EmailVerificationToken = resetToken;
            user.ApprovalStatus = ApprovalStatuses.Approved;
            user.MustChangePassword = true;
            //user.OrgId = applicationUserInviteRequest.OrganizationId;
            user.Organization = org;


            var result = await _userManager.CreateAsync(user, "_Otp-" + randomPassword);

            //Map the user to role

            var isInRole = await _userManager.IsInRoleAsync(user, role);
            if (!isInRole)
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            if (result == null)
                return CustomResult<ApplicationUserSignUpResponse>.Failure(CustomError.UserInvitationFailed, ResponseCodes.UserInvitationFailedErrorCode);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                var firstError = errors.FirstOrDefault();
                return CustomResult<ApplicationUserSignUpResponse>.Failure(new CustomError(firstError), ResponseCodes.UserInvitationFailedErrorCode);
            }


            var mappedData = _mapper.Map<ApplicationUserSignUpResponse>(user);

            //TODO: Send the invite mail to the user\
            var mailBody = EmailTemplate.OTPEmail;
            mailBody = mailBody.Replace("{{OrgEMail}}", "info@thels1.com").Replace("{{OTP1}}", randomPassword[0].ToString())
                .Replace("{{OTP2}}", randomPassword[1].ToString()).Replace("{{OTP3}}", randomPassword[2].ToString())
                .Replace("{{OTP4}}", randomPassword[3].ToString()).Replace("{{OTP5}}", randomPassword[4].ToString())
                .Replace("{{OTP6}}", randomPassword[5].ToString());
            string mailSubject = EmailTemplate.OTPEmailSubject + $" - {randomPassword}";
            SendEmailRequestDto model = new SendEmailRequestDto
            {
                EmailBody = mailBody,
                Subject = mailSubject,
                RecipientEmail = user.Email
            };
            //await _emailService.SendMailAsync(model);
            //TODO: Handle SendGrid Error
            return CustomResult<ApplicationUserSignUpResponse>.Success(mappedData);

        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            _logger.LogError(ex, msg);
            return CustomResult<ApplicationUserSignUpResponse>.Failure(new CustomError(msg), ResponseCodes.OperationError);
        }
    }

    public async Task<CustomResult<ApplicationUserResponse>> DeleteAsync(Guid id)
    {
        try
        {


            var existingRecord = await GetUserByIdAsync(id.ToString());

            if (existingRecord == null)
                return CustomResult<ApplicationUserResponse>.Failure(CustomError.AccountNotFoundError, ResponseCodes.AccountNotFoundErrorCode);

            var result = await _userManager.DeleteAsync(existingRecord);

            if (!result.Succeeded)
            {
                _logger.LogInformation("User account with username {UserName} could not be deleted", existingRecord.UserName);
                var msg = result.Errors.Select(x => x.Description).FirstOrDefault() ?? ResponseMessages.AccountNotFoundErrorMessage;
                return CustomResult<ApplicationUserResponse>.Failure(new CustomError(msg), ResponseCodes.AccountNotFoundErrorCode);
            }

            var mappedData = _mapper.Map<ApplicationUserResponse>(existingRecord);
            _logger.LogInformation("User account with username {UserName} was deleted successfully. Details: {@data}", existingRecord.UserName, mappedData);
            return CustomResult<ApplicationUserResponse>.Success(mappedData);

        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            _logger.LogError(ex, msg);
            return CustomResult<ApplicationUserResponse>.Failure(new CustomError(msg), ResponseCodes.OperationError);
        }
    }

    public async Task<CustomResult<ApplicationUserResponse>> DisableAsync(Guid id)
    {
        ApplicationUserResponse responseData = null!;
        try
        {

            var existingRecord = await GetUserByIdAsync(id.ToString());

            if (existingRecord == null)
                return CustomResult<ApplicationUserResponse>.Failure(CustomError.AccountNotFoundError, ResponseCodes.AccountNotFoundErrorCode);

            if (existingRecord.IsActive)
            {
                existingRecord.IsActive = false;
                existingRecord.DateLastUpdated = DateTime.UtcNow;
                var result = await _userManager.UpdateAsync(existingRecord);

                if (result.Succeeded)
                {
                    responseData = _mapper.Map<ApplicationUserResponse>(existingRecord);
                    _logger.LogInformation("User account with username {UserName} was disabled successfully.", existingRecord.UserName);
                    return CustomResult<ApplicationUserResponse>.Success(responseData);
                }
                else
                {
                    _logger.LogInformation($"User account with username {existingRecord.UserName} could not be disabled. ");
                    return CustomResult<ApplicationUserResponse>.Failure(CustomError.DisableAccountError, ResponseCodes.DisableAccountErrorCode);
                }
            }

            _logger.LogInformation("User account with username {UserName} was not disabled. No action was done as account is already disabled", existingRecord.UserName);
            responseData = _mapper.Map<ApplicationUserResponse>(existingRecord);
            return CustomResult<ApplicationUserResponse>.Success(responseData);


        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            _logger.LogError(ex, msg);
            return CustomResult<ApplicationUserResponse>.Failure(new CustomError(msg), ResponseCodes.OperationError);
        }
    }

    public async Task<CustomResult<ApplicationUserResponse>> EnableAsync(Guid id)
    {
        ApplicationUserResponse responseData = null;
        var existingRecord = await GetUserByIdAsync(id.ToString());

        if (existingRecord == null)
            return CustomResult<ApplicationUserResponse>.Failure(CustomError.AccountNotFoundError, ResponseCodes.AccountNotFoundErrorCode);

        if (!existingRecord.IsActive)
        {
            existingRecord.IsActive = true;
            existingRecord.DateLastUpdated = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(existingRecord);

            if (result.Succeeded)
            {
                responseData = _mapper.Map<ApplicationUserResponse>(existingRecord);
                _logger.LogInformation("User account with username {UserName} was enabled successfully.", existingRecord.UserName);
                return CustomResult<ApplicationUserResponse>.Success(responseData);
            }
            else
            {
                _logger.LogInformation("User account with username {UserName} could not be enabled.", existingRecord.UserName);
                return CustomResult<ApplicationUserResponse>.Failure(CustomError.EnableAccountError, ResponseCodes.EnableAccountErrorCode);
            }
        }

        _logger.LogInformation($"User account with username {existingRecord.UserName} was not enabled. No action was done as account is already enabled");
        responseData = _mapper.Map<ApplicationUserResponse>(existingRecord);
        return CustomResult<ApplicationUserResponse>.Success(responseData);
    }

    public async Task<CustomResult<ApplicationUserResponse>> GetItemAsync(Guid id)
    {
        var existingRecord = await _userManager.Users
                .FirstOrDefaultAsync(x => x.Id == id);

        if (existingRecord != null)
        {
            _logger.LogInformation("User account with username {UserName} was retrieved successfully.", existingRecord.UserName);
            var mappedData = _mapper.Map<ApplicationUserResponse>(existingRecord);
            return CustomResult<ApplicationUserResponse>.Success(mappedData);
        }

        _logger.LogInformation("User account with Id {Id} was could not be retrieved", id);
        return CustomResult<ApplicationUserResponse>.Success(null!, ResponseCodes.NoRecordFoundCode);
    }

    public async Task<CustomResult<List<ApplicationUserResponse>>> GetUsersAsync()
    {
        var existingRecords = await _userManager.Users
                .Where(x => x.IsActive).ToListAsync();

        if (existingRecords != null && existingRecords.Count > 0)
        {
            var mappedList = _mapper.Map<List<ApplicationUserResponse>>(existingRecords);
            _logger.LogInformation("{Count} user records were retrieved successfully.", existingRecords.Count);
            return CustomResult<List<ApplicationUserResponse>>.Success(mappedList);
        }

        _logger.LogInformation("No user records found");
        return CustomResult<List<ApplicationUserResponse>>.Success(new List<ApplicationUserResponse>(), ResponseCodes.NoRecordFoundCode);
    }
    public async Task<CustomResult<List<ApplicationRoleResponse>>> GetRolesAsync()
    {
        var existingRecords = await _roleManager.Roles
                //.Where(x => x.IsActive)
                .ToListAsync();

        if (existingRecords != null && existingRecords.Count > 0)
        {
            var mappedList = _mapper.Map<List<ApplicationRoleResponse>>(existingRecords);
            _logger.LogInformation("{Count} Roles records were retrieved successfully.", existingRecords.Count);
            return CustomResult<List<ApplicationRoleResponse>>.Success(mappedList);
        }

        _logger.LogInformation("No role records found");
        return CustomResult<List<ApplicationRoleResponse>>.Success(new List<ApplicationRoleResponse>(), ResponseCodes.NoRecordFoundCode);
    }

    public async Task<CustomResult<ApplicationUserResponse>> UpdateAsync(Guid id, ApplicationUserUpdateRequest applicationUserUpdateRequest, DateTime currentDateTime)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
        {
            _logger.LogError("User with id {id} does not exist", id);
            return CustomResult<ApplicationUserResponse>.Failure(CustomError.AccountNotFoundError, ResponseCodes.AccountNotFoundErrorCode);
        }

        // Update DisplayName if provided
        if (!string.IsNullOrWhiteSpace(applicationUserUpdateRequest.FirstName) ||
            !string.IsNullOrWhiteSpace(applicationUserUpdateRequest.LastName))
        {
            user.DisplayName = $"{applicationUserUpdateRequest.FirstName} {applicationUserUpdateRequest.LastName}";
            user.FullName = $"{applicationUserUpdateRequest.FirstName} {applicationUserUpdateRequest.LastName}";
        }

        // Update PhoneNumber if provided
        if (!string.IsNullOrWhiteSpace(applicationUserUpdateRequest.PhoneNumber))
        {
            user.PhoneNumber = applicationUserUpdateRequest.PhoneNumber;
        }

        user.Address = applicationUserUpdateRequest.Address;
        user.Street = applicationUserUpdateRequest.Street;
        user.State = applicationUserUpdateRequest.State;
        user.City = applicationUserUpdateRequest.City;
        user.ZipCode = applicationUserUpdateRequest.ZipCode;


        // Update Email if provided and unique
        //if (!string.IsNullOrWhiteSpace(applicationUserUpdateRequest.Email))
        //{
        //    var emailExist = await _userManager.FindByEmailAsync(applicationUserUpdateRequest.Email);

        //    if (emailExist != null)
        //    {
        //        return CustomResult<ApplicationUserResponse>.Failure(CustomError.EmailAlreadyInUse, ResponseCodes.EmailAlreadyInUse);
        //    }

        //    user.Email = applicationUserUpdateRequest.Email;
        //}

        // Update RoleId if provided and different from the current role
        //if (applicationUserUpdateRequest.RoleId.HasValue && user.RoleId != applicationUserUpdateRequest.RoleId.Value)
        //{
        //    var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == applicationUserUpdateRequest.RoleId.Value);

        //    if (role != null)
        //    {
        //        user.RoleId = applicationUserUpdateRequest.RoleId.Value;
        //    }
        //    else
        //    {
        //        return CustomResult<ApplicationUserResponse>.Failure(CustomError.RoleNotFoundError, ResponseCodes.RoleNotFoundErrorCode);
        //    }
        //}


        //upload user passport if any 
        var passportUrl = "";
        if (applicationUserUpdateRequest.Passport != null)
        {


            using var fileStream = applicationUserUpdateRequest.Passport.OpenReadStream();
            var passportuploadResult = await _utilityService.UploadFileToS3(new UploadFileRequestDataDto
            {
                FileStream = fileStream,
                Filename = user.FirebaseId,
                FileContentType = applicationUserUpdateRequest.Passport.ContentType,
                FileExtension = Path.GetExtension(applicationUserUpdateRequest.Passport.FileName),
            });
            passportUrl = passportuploadResult.Data;
        }

        user.ProfilePixUrl = passportUrl;
        // Update user data
        var updateResult = await _userManager.UpdateAsync(user);

        if (updateResult.Succeeded)
        {
            var mappedData = _mapper.Map<ApplicationUser, ApplicationUserResponse>(user);
            return CustomResult<ApplicationUserResponse>.Success(mappedData);
        }

        // In case of failure, log the errors and return failure
        _logger.LogError("Failed to update user with id {id}: {Errors}", id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        return CustomResult<ApplicationUserResponse>.Failure(CustomError.AccountUpdateError, ResponseCodes.AccountUpdateErrorCode);
    }

    public async Task<CustomResult<ApplicationUserResponse>> ApproveAsync(Guid userId, ApprovalRequest approvalRequest, string currentUserId, DateTime currentDateTime)
    {
        var existingRecord = await GetUserByIdAsync(userId.ToString());

        if (existingRecord == null)
            return CustomResult<ApplicationUserResponse>.Failure(CustomError.AccountNotFoundError, ResponseCodes.AccountNotFoundErrorCode);

        if (existingRecord.ApprovalStatus != ApprovalStatuses.Pending)
            return CustomResult<ApplicationUserResponse>.Failure(CustomError.ApprovalActionAlreadyTaken, ResponseCodes.ApprovalActionAlreadyTaken);

        _logger.LogInformation($"Approving user account with email {existingRecord.Email}. Details: {JsonConvert.SerializeObject(approvalRequest)} ");

        existingRecord.ApprovalStatus = ApprovalStatuses.Approved;
        //existingRecord.ApprovalActionDate = currentDateTime;
        //existingRecord.ApprovalActionReason = approvalRequest.Comment;
        //existingRecord.ApprovalActionBy = currentUserId;
        existingRecord.DateLastUpdated = currentDateTime;

        //await UpdateAsync(userId, existingRecord);

        var mappedData = _mapper.Map<ApplicationUserResponse>(existingRecord);
        _logger.LogInformation($"User with email {existingRecord.UserName} was approved successfully. Details: {JsonConvert.SerializeObject(mappedData)} ");
        return CustomResult<ApplicationUserResponse>.Success(mappedData);
    }

    public async Task<CustomResult<ApplicationUserResponse>> RejectAsync(Guid userId, ApprovalRequest approvalRequest, string currentUserId, DateTime currentDateTime)
    {
        var existingRecord = await GetUserByIdAsync(userId.ToString());

        if (existingRecord == null)
            return CustomResult<ApplicationUserResponse>.Failure(CustomError.AccountNotFoundError, ResponseCodes.AccountNotFoundErrorCode);

        if (existingRecord.ApprovalStatus != ApprovalStatuses.Pending)
            return CustomResult<ApplicationUserResponse>.Failure(CustomError.ApprovalActionAlreadyTaken, ResponseCodes.ApprovalActionAlreadyTaken);

        existingRecord.ApprovalStatus = ApprovalStatuses.Rejected;
        existingRecord.DateLastUpdated = currentDateTime;

        //await UpdateAsync(userId, existingRecord, currentDateTime);
        var mappedData = _mapper.Map<ApplicationUserResponse>(existingRecord);
        _logger.LogInformation($"User account with email {existingRecord.Email} was rejected. Details: {JsonConvert.SerializeObject(mappedData)} ");
        return CustomResult<ApplicationUserResponse>.Success(mappedData);
    }

    public async Task<CustomResult<PagedList<ApplicationUserResponse>>> ApprovedListAsync(DateRangeQueryModel query)
    {
        IQueryable<ApplicationUser> tempQuery;

        tempQuery = _userManager.Users
            .Where(x => x.ApprovalStatus == ApprovalStatuses.Approved);

        if (query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
               .Where(x => x.DateCreated >= query.StartDate.Value && x.DateCreated <= query.EndDate.Value);
        }
        else if (query.StartDate.HasValue && !query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated >= query.StartDate.Value);
        }
        else if (!query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated <= query.EndDate.Value);
        }

        IQueryable<ApplicationUserResponse> entityQuery = EntitySelectSearch(tempQuery);
        entityQuery = EntityFilterSearch(entityQuery, query).OrderByDescending(x => x.DateCreated);
        var paginatedData = await entityQuery.Paginate(query.PageNumber, query.PageSize).ToListAsync();

        int count = entityQuery.Count();
        var pagedList = new PagedList<ApplicationUserResponse>(paginatedData, query.PageNumber, query.PageSize, count);

        _logger.LogInformation("Approved user accounts retrieved successfully. {Count} record(s) found", pagedList.TotalPageCount);
        return CustomResult<PagedList<ApplicationUserResponse>>.Success(pagedList);
    }

    public async Task<CustomResult<PagedList<ApplicationUserResponse>>> PendingListAsync(DateRangeQueryModel query)
    {
        IQueryable<ApplicationUser> tempQuery;
        tempQuery = _userManager.Users
            .Where(x => x.ApprovalStatus == ApprovalStatuses.Pending);

        if (query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
               .Where(x => x.DateCreated >= query.StartDate.Value && x.DateCreated <= query.EndDate.Value);
        }
        else if (query.StartDate.HasValue && !query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated >= query.StartDate.Value);
        }
        else if (!query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated <= query.EndDate.Value);
        }

        IQueryable<ApplicationUserResponse> entityQuery = EntitySelectSearch(tempQuery);
        entityQuery = EntityFilterSearch(entityQuery, query).OrderByDescending(x => x.DateCreated);

        var paginatedData = await entityQuery.Paginate(query.PageNumber, query.PageSize).ToListAsync();
        int count = entityQuery.Count();
        var pagedList = new PagedList<ApplicationUserResponse>(paginatedData, query.PageNumber, query.PageSize, count);
        _logger.LogInformation("Approved user accounts retrieved successfully. {Count} record(s) found", pagedList.TotalPageCount);
        return CustomResult<PagedList<ApplicationUserResponse>>.Success(pagedList);
    }

    public async Task<CustomResult<PagedList<ApplicationUserResponse>>> RejectedListAsync(DateRangeQueryModel query)
    {
        IQueryable<ApplicationUser> tempQuery;

        tempQuery = _userManager.Users
            .Where(x => x.ApprovalStatus == ApprovalStatuses.Rejected);

        if (query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
               .Where(x => x.DateCreated >= query.StartDate.Value && x.DateCreated <= query.EndDate.Value);
        }
        else if (query.StartDate.HasValue && !query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated >= query.StartDate.Value);
        }
        else if (!query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated <= query.EndDate.Value);
        }

        IQueryable<ApplicationUserResponse> entityQuery = EntitySelectSearch(tempQuery);

        entityQuery = EntityFilterSearch(entityQuery, query).OrderByDescending(x => x.ApprovalActionDate);
        var paginatedData = await entityQuery.Paginate(query.PageNumber, query.PageSize).ToListAsync();

        int count = entityQuery.Count();
        var pagedList = new PagedList<ApplicationUserResponse>(paginatedData, query.PageNumber, query.PageSize, count);
        _logger.LogInformation("Rejected user accounts retrieved successfully. {Count} record(s) found", pagedList.TotalPageCount);
        return CustomResult<PagedList<ApplicationUserResponse>>.Success(pagedList);
    }

    public async Task<CustomResult<PagedList<ApplicationUserResponse>>> SearchAsync(DateRangeQueryModel query)
    {
        IQueryable<ApplicationUser> tempQuery;
        tempQuery = _userManager.Users.AsQueryable();

        if (query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
               .Where(x => x.DateCreated >= query.StartDate.Value && x.DateCreated <= query.EndDate.Value);
        }
        else if (query.StartDate.HasValue && !query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated >= query.StartDate.Value);
        }
        else if (!query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated <= query.EndDate.Value);
        }

        IQueryable<ApplicationUserResponse> entityQuery = EntitySelectSearch(tempQuery);
        entityQuery = EntityFilterSearch(entityQuery, query).OrderByDescending(x => x.DateCreated);
        var paginatedData = await entityQuery.Paginate(query.PageNumber, query.PageSize).ToListAsync();
        int count = entityQuery.Count();
        var pagedList = new PagedList<ApplicationUserResponse>(paginatedData, query.PageNumber, query.PageSize, count);
        _logger.LogInformation("User account search completed successfully. {Count} record(s) found", pagedList.TotalPageCount);
        return CustomResult<PagedList<ApplicationUserResponse>>.Success(pagedList);
    }

    public async Task<CustomResult<List<ApplicationUserResponse>>> ExportDataAsync(ExportQueryModel query)
    {
        IQueryable<ApplicationUser> tempQuery;

        tempQuery = _userManager.Users;

        if (query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
               .Where(x => x.DateCreated >= query.StartDate.Value && x.DateCreated <= query.EndDate.Value);
        }
        else if (query.StartDate.HasValue && !query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated >= query.StartDate.Value);
        }
        else if (!query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated <= query.EndDate.Value);
        }

        IQueryable<ApplicationUserResponse> entityQuery = EntitySelectSearch(tempQuery);

        entityQuery = EntityFilterSearch(entityQuery, query);
        entityQuery = entityQuery.OrderByDescending(x => x.ApprovalActionDate);
        List<ApplicationUserResponse> list = await entityQuery.ToListAsync();
        _logger.LogInformation("User data export completed successfully. {Count} record(s) found", list.Count());
        return CustomResult<List<ApplicationUserResponse>>.Success(list);
    }

    public async Task<CustomResult<CountAnalyticResponse>> GetTotalNumberOfRolesAsync()
    {
        IQueryable<ApplicationRole> tempQuery2;
        long count = 0;

        var tempQuery = _roleManager.Roles.AsQueryable();
        count = await tempQuery.CountAsync();

        CountAnalyticResponse data = new()
        {
            Count = count,
            FormattedCount = count.ToString("N0")
        };

        return CustomResult<CountAnalyticResponse>.Success(data);
    }

    public async Task<CustomResult<CountAnalyticResponse>> GetTotalNumberOfUsersApprovedAsync(DateRangeQueryModel queryModel)
    {
        //IQueryable<UserManagementRequest> tempQuery;
        //long count = 0;

        //tempQuery = _userManagementRequestRepo.GetAll()
        //    .Where(x => x.ApprovalStatus == ApprovalStatuses.Approved && x.EntityManagementRequestType == EntityManagementRequestTypes.USER_INVITE && x.IsActive);

        IQueryable<ApplicationUser> tempQuery;
        long count = 0;

        tempQuery = _userManager.Users
            .Where(x => x.ApprovalStatus == ApprovalStatuses.Approved && x.IsActive);

        if (queryModel == null)
        {
            count = await tempQuery.CountAsync();
        }
        else
        {
            if (queryModel.StartDate.HasValue && queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                   .Where(x => x.DateCreated.Date >= queryModel.StartDate.Value && x.DateCreated.Date <= queryModel.EndDate.Value);
            }
            else if (queryModel.StartDate.HasValue && !queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.DateCreated >= queryModel.StartDate.Value);
            }
            else if (!queryModel.StartDate.HasValue && queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.DateCreated <= queryModel.EndDate.Value);
            }

            count = await tempQuery.CountAsync();
        }

        CountAnalyticResponse data = new()
        {
            Count = count,
            FormattedCount = count.ToString("N0")
        };

        return CustomResult<CountAnalyticResponse>.Success(data);
    }

    public async Task<CustomResult<CountAnalyticResponse>> GetTotalNumberOfUsersAsync(DateRangeQueryModel queryModel)
    {
        IQueryable<ApplicationUser> tempQuery;
        long count = 0;

        tempQuery = _userManager.Users
            .Where(x => x.IsActive);

        if (queryModel == null)
        {
            count = await tempQuery.CountAsync();
        }
        else
        {
            if (queryModel.StartDate.HasValue && queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                   .Where(x => x.DateCreated.Date >= queryModel.StartDate.Value && x.DateCreated.Date <= queryModel.EndDate.Value);
            }
            else if (queryModel.StartDate.HasValue && !queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.DateCreated >= queryModel.StartDate.Value);
            }
            else if (!queryModel.StartDate.HasValue && queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.DateCreated <= queryModel.EndDate.Value);
            }

            count = await tempQuery.CountAsync();
        }

        CountAnalyticResponse data = new()
        {
            Count = count,
            FormattedCount = count.ToString("N0")
        };

        return CustomResult<CountAnalyticResponse>.Success(data);
    }

    public async Task<CustomResult<CountAnalyticResponse>> GetTotalNumberOfUsersPendingApprovalAsync(DateRangeQueryModel queryModel)
    {
        IQueryable<ApplicationUser> tempQuery;
        long count = 0;

        tempQuery = _userManager.Users
            .Where(x => x.ApprovalStatus == ApprovalStatuses.Pending);

        if (queryModel == null)
        {
            count = await tempQuery.CountAsync();
        }
        else
        {
            if (queryModel.StartDate.HasValue && queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                   .Where(x => x.DateCreated.Date >= queryModel.StartDate.Value && x.DateCreated.Date <= queryModel.EndDate.Value);
            }
            else if (queryModel.StartDate.HasValue && !queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.DateCreated >= queryModel.StartDate.Value);
            }
            else if (!queryModel.StartDate.HasValue && queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.DateCreated <= queryModel.EndDate.Value);
            }

            count = await tempQuery.CountAsync();
        }

        CountAnalyticResponse data = new CountAnalyticResponse()
        {
            Count = count,
            FormattedCount = count.ToString("N0")
        };
        return CustomResult<CountAnalyticResponse>.Success(data);
    }

    public async Task<CustomResult<CountAnalyticResponse>> GetTotalNumberOfUsersRejectedAsync(DateRangeQueryModel queryModel)
    {
        IQueryable<ApplicationUser> tempQuery;
        long count = 0;

        tempQuery = _userManager.Users
            .Where(x => x.ApprovalStatus == ApprovalStatuses.Rejected);

        if (queryModel == null)
        {
            count = await tempQuery.CountAsync();
        }
        else
        {
            if (queryModel.StartDate.HasValue && queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                   .Where(x => x.DateCreated.Date >= queryModel.StartDate.Value && x.DateCreated.Date <= queryModel.EndDate.Value);
            }
            else if (queryModel.StartDate.HasValue && !queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.DateCreated >= queryModel.StartDate.Value);
            }
            else if (!queryModel.StartDate.HasValue && queryModel.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.DateCreated <= queryModel.EndDate.Value);
            }

            count = await tempQuery.CountAsync();
        }

        CountAnalyticResponse data = new CountAnalyticResponse()
        {
            Count = count,
            FormattedCount = count.ToString("N0")
        };

        return CustomResult<CountAnalyticResponse>.Success(data);
    }

    public async Task<CustomResult<ApplicationUserResponseDto>> GetLoggedInUser()
    {
        try
        {
            var userClaims = await _tokenService.GetUserClaim();

            if (userClaims == null || string.IsNullOrWhiteSpace(userClaims?.Email))
            {
                _logger.LogError("Invalid user claims.... user claims null");
                return CustomResult<ApplicationUserResponseDto>.Failure(CustomError.UserClaimsError, ResponseCodes.InvalidUserClaims);
            }

            var user = await _userManager.Users
                .Where(x => x.Email == userClaims.Email)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogError("User with email address {@email} does not exist", userClaims.Email);
                return CustomResult<ApplicationUserResponseDto>.Failure(CustomError.UserNotFound, ResponseCodes.UserNotFound);
            }

            var userData = new ApplicationUserResponseDto()
            {
                UserId = user.Id,
                RoleId = user.RoleId,
                FullName = user.FullName,
                Email = user.Email,
                FirebaseId = user.FirebaseId,
                SAPUserId = user.SAPUserId,
                OrgId = user.OrgId
            };

            _logger.LogInformation("Logged in user info: {@info}", userData);
            return CustomResult<ApplicationUserResponseDto>.Success(userData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(ApplicationUserService).Name, nameof(GetLoggedInUser));
            throw;
        }
    }

    public async Task<CustomResult<string>> GetLoggedInUserEmail()
    {
        try
        {
            var userClaims = await _tokenService.GetUserClaim();

            if (userClaims == null || string.IsNullOrWhiteSpace(userClaims?.Email))
            {
                _logger.LogError("Invalid user claims.... user claims null");
                return CustomResult<string>.Failure(CustomError.UserClaimsError, ResponseCodes.InvalidUserClaims);
            }

            return CustomResult<string>.Success(userClaims?.Email!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(ApplicationUserService).Name, nameof(GetLoggedInUserEmail));
            throw;
        }
    }

    #region "Private Functions"
    private IQueryable<ApplicationUserResponse> EntitySelectSearch(IQueryable<ApplicationUser> query)
    {
        return query.Select(x => new ApplicationUserResponse()
        {
            Id = x.Id,
            UserName = x.UserName,
            DisplayName = x.DisplayName,
            Email = x.Email,
            PhoneNumber = x.PhoneNumber,
            RoleId = x.RoleId,
            LastSignInDate = x.LastSignInDate,
            IsActive = x.IsActive,
            ApprovalStatus = x.ApprovalStatus,
            DateCreated = x.DateCreated
        });
    }

    private IQueryable<ApplicationUserResponse> EntityFilterSearch(IQueryable<ApplicationUserResponse> query, QueryModel model)
    {
        if (model != null && !string.IsNullOrEmpty(model.Filter) && !string.IsNullOrEmpty(model.Keyword))
        {
            var keyWord = model.Keyword.Trim().ToLower();

            switch (model.Filter.ToLower())
            {
                case "userid":
                    {
                        query = query.Where(x => x.Id.ToString().ToLower() == keyWord);
                        break;
                    }
                case "username":
                    {
                        query = query.Where(x => x.UserName == keyWord);
                        break;
                    }
                case "email":
                    {
                        query = query.Where(x => x.Email == keyWord);
                        break;
                    }
                case "phonenumber":
                    {
                        query = query.Where(x => x.PhoneNumber == keyWord);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        return query;
    }

    private async Task<ApplicationUser> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user!;
    }

    private async Task<ApplicationUser> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user!;
    }

    private IQueryable<ApplicationUserResponse> EntityFilterSearch(IQueryable<ApplicationUserResponse> query, ExportQueryModel model)
    {
        if (model != null && query != null && !string.IsNullOrEmpty(model.Filter) && !string.IsNullOrEmpty(model.Keyword))
        {
            var keyWord = model.Keyword.Trim().ToLower();

            switch (model.Filter.ToLower())
            {
                case "userid":
                    {
                        query = query.Where(x => x.Id.ToString().ToLower() == keyWord);
                        break;
                    }
                case "username":
                    {
                        query = query.Where(x => x.UserName == keyWord);
                        break;
                    }
                case "email":
                    {
                        query = query.Where(x => x.Email == keyWord);
                        break;
                    }
                case "phonenumber":
                    {
                        query = query.Where(x => x.PhoneNumber == keyWord);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        return query;
    }

    public async Task<CustomResult<List<ApplicationUserResponse>>> ExportDataApprovedListAsync(ExportQueryModel query)
    {
        IQueryable<ApplicationUser> tempQuery;

        tempQuery = _userManager.Users
            .Where(x => x.ApprovalStatus == ApprovalStatuses.Approved);

        if (query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
               .Where(x => x.DateCreated >= query.StartDate.Value && x.DateCreated <= query.EndDate.Value);
        }
        else if (query.StartDate.HasValue && !query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated >= query.StartDate.Value);
        }
        else if (!query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated <= query.EndDate.Value);
        }

        IQueryable<ApplicationUserResponse> entityQuery = EntitySelectSearch(tempQuery);

        entityQuery = EntityFilterSearch(entityQuery, query);

        entityQuery = entityQuery.OrderByDescending(x => x.ApprovalActionDate);

        List<ApplicationUserResponse> list = await entityQuery.ToListAsync();

        _logger.LogInformation($"Approved User requests export completed successfully. {list.Count()} record(s) found ");

        return CustomResult<List<ApplicationUserResponse>>.Success(list);
    }

    public async Task<CustomResult<List<ApplicationUserResponse>>> ExportDataRejectedListAsync(ExportQueryModel query)
    {
        if (query == null)
        {
            return CustomResult<List<ApplicationUserResponse>>.Failure(CustomError.ParameterInputNotProvided, ResponseCodes.ParameterInputNotProvided);
        }

        IQueryable<ApplicationUser> tempQuery;

        tempQuery = _userManager.Users
            .Where(x => x.ApprovalStatus == ApprovalStatuses.Rejected);

        if (query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
               .Where(x => x.DateCreated >= query.StartDate.Value && x.DateCreated <= query.EndDate.Value);
        }
        else if (query.StartDate.HasValue && !query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated >= query.StartDate.Value);
        }
        else if (!query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated <= query.EndDate.Value);
        }

        IQueryable<ApplicationUserResponse> entityQuery = EntitySelectSearch(tempQuery);

        entityQuery = EntityFilterSearch(entityQuery, query);

        entityQuery = entityQuery.OrderByDescending(x => x.ApprovalActionDate);

        List<ApplicationUserResponse> list = await entityQuery.ToListAsync();
        _logger.LogInformation($"Rejected User requests export completed successfully. {list.Count()} record(s) found");
        return CustomResult<List<ApplicationUserResponse>>.Success(list);
    }

    public async Task<CustomResult<List<ApplicationUserResponse>>> ExportDataPendingListAsync(ExportQueryModel query)
    {
        if (query == null)
        {
            return CustomResult<List<ApplicationUserResponse>>.Failure(CustomError.ParameterInputNotProvided, ResponseCodes.ParameterInputNotProvided);
        }

        IQueryable<ApplicationUser> tempQuery;


        tempQuery = _userManager.Users
            .Where(x => x.ApprovalStatus == ApprovalStatuses.Pending);

        if (query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
               .Where(x => x.DateCreated >= query.StartDate.Value && x.DateCreated <= query.EndDate.Value);
        }
        else if (query.StartDate.HasValue && !query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated >= query.StartDate.Value);
        }
        else if (!query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated <= query.EndDate.Value);
        }

        IQueryable<ApplicationUserResponse> entityQuery = EntitySelectSearch(tempQuery);
        entityQuery = EntityFilterSearch(entityQuery, query);
        entityQuery = entityQuery.OrderByDescending(x => x.ApprovalActionDate);

        List<ApplicationUserResponse> list = await entityQuery.ToListAsync();
        _logger.LogInformation($"Approved User requests export completed successfully. {list.Count()} record(s) found ");
        return CustomResult<List<ApplicationUserResponse>>.Success(list);
    }

    private async Task<CustomResult<ADUserProfileResponse>> GetUserProfileAsync(string email)
    {
        _logger.LogInformation($"Getting user profile for {email}");

        string url = $"{_config.GetValue<string>("ServiceEndpoints:ADUserProfileUrl")}";
        url = url.Replace("{email}", email);
        var response = await _httpClientService.MakeHttpCall(HttpMethod.Get, url);
        var responseContent = await response.Content.ReadAsStringAsync();

        var responseData = JsonConvert.DeserializeObject<ApiResponse<ADUserProfileResponse>>(responseContent);

        if (responseData != null && responseData.Data != null && response.IsSuccessStatusCode && responseData.Code == "00")
        {
            _logger.LogInformation($"Getting user profile for {email} Successful");
            return CustomResult<ADUserProfileResponse>.Success(responseData.Data);
        }
        else
        {
            _logger.LogInformation($"Getting user profile for {email} failed");
            return CustomResult<ADUserProfileResponse>.Failure(CustomError.UnableToRetrieveUserProfile, ResponseCodes.UnableToRetrieveUserProfile);
        }
    }

    public async Task<CustomResult<PagedList<ApplicationUserResponse>>> GetUsersListByRoleAsync(DateRangeQueryModel query, Guid roleId)
    {
        IQueryable<ApplicationUser> tempQuery;

        tempQuery = _userManager.Users
            .Where(x => x.RoleId == roleId);

        if (query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
               .Where(x => x.DateCreated >= query.StartDate.Value && x.DateCreated <= query.EndDate.Value);
        }
        else if (query.StartDate.HasValue && !query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated >= query.StartDate.Value);
        }
        else if (!query.StartDate.HasValue && query.EndDate.HasValue)
        {
            tempQuery = tempQuery
                .Where(x => x.DateCreated <= query.EndDate.Value);
        }

        IQueryable<ApplicationUserResponse> entityQuery = EntitySelectSearch(tempQuery);
        entityQuery = EntityFilterSearch(entityQuery, query).OrderByDescending(x => x.DateCreated);
        var paginatedData = await entityQuery.Paginate(query.PageNumber, query.PageSize).ToListAsync();

        int count = entityQuery.Count();
        var pagedList = new PagedList<ApplicationUserResponse>(paginatedData, query.PageNumber, query.PageSize, count);

        _logger.LogInformation("Approved user accounts retrieved successfully. {Count} record(s) found", pagedList.TotalPageCount);
        return CustomResult<PagedList<ApplicationUserResponse>>.Success(pagedList);

    }
    #endregion
}