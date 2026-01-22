using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using qguardbackend.Core.Helpers;
using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.EmailDtos;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using qguardbackend.Data.Enums.Constants;
//using qguardbackend.Data.Migrations;
using qguardbackend.Api.ServiceExtensions;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Model;
using Firebase.Auth;
using FluentEmail.Core;
using qguardbackend.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using qguardbackend.BoilerPlate.Service.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace qguardbackend.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthService> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUserManagementService _userMgmtService;
        //private readonly IAuditLogService _auditLogSvc;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IS3Service _s3Service;
        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<AuthService> logger, IEmailService emailService,
            SignInManager<ApplicationUser> signInManager,
            IUserManagementService userMgmtService,
                 IS3Service s3Service,
            //IAuditLogService auditLogSvc,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _signInManager = signInManager;
            _userMgmtService = userMgmtService;
            //_auditLogSvc = auditLogSvc;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _emailService = emailService;
            _s3Service = s3Service;
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

        public async Task<CustomResult<ReturnTokenModel>> LoginAsync(LoginRequestDto model)
        {
            try
            {
                var InsTId = await _userMgmtService.GetTenantId();
                if (!InsTId.IsSuccess) return CustomResult<ReturnTokenModel>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                    return CustomResult<ReturnTokenModel>.ErrorOccured("Email/Password is required!", ResponseCodes.BadRequestErrorCode);

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == model.Email.ToLower() /*&& x.InstitutionId == InsTId.Data*/);
                if (user == null) return CustomResult<ReturnTokenModel>.ErrorOccured("Invalid email address!", ResponseCodes.BadRequestErrorCode);


                if (!await _userManager.CheckPasswordAsync(user, model.Password))
                    return CustomResult<ReturnTokenModel>.Failure(CustomError.InvalidUsernameOrPassword, ResponseCodes.InvalidUsernameOrPassword);
                //return CustomResult<ReturnTokenModel>.ErrorOccured(CustomError.InvalidUsernameOrPassword, ResponseCodes.InvalidUsernameOrPassword);

                //check if the user requires to change password
                if (user.RequiresPasswordChange)
                    return CustomResult<ReturnTokenModel>.Failure(CustomError.RequiresPasswordChange, ResponseCodes.RequiresPasswordChange);


                //check if the user is profiled to have access to the institution
                var hasAccess = await _context.UserRoles
                 .FirstOrDefaultAsync(x => x.UserId == user.Id //&& x.InstitutionId == InsTId.Data
                 );

                var hasAccessAsSystemAdmin = await _context.SystemAdminOtherTenantsRole
                  .AnyAsync(x => x.UserId == user.Id && x.InstitutionId == InsTId.Data);

                if (hasAccess is null && !hasAccessAsSystemAdmin)
                {
                    return CustomResult<ReturnTokenModel>.ErrorOccured("User is not permitted to access this tenant, check the tenant code!", ResponseCodes.BadRequestErrorCode);
                }

                if (!_context.Users.FirstOrDefaultAsync(x => x.Id == user.Id).Result.EmailConfirmed)
                    return CustomResult<ReturnTokenModel>.ErrorOccured("Please confirm your account to log in", ResponseCodes.BadRequestErrorCode);

                if (!await _userMgmtService.CheckIfAccountIsActive(user.Id))
                    return CustomResult<ReturnTokenModel>.ErrorOccured("Your account has been blocked", ResponseCodes.BadRequestErrorCode);

                var userRoles = new List<string>();
                var roles = await _userManager.GetRolesAsync(user);
                userRoles.AddRange(roles);
                //var insTdetails = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == InsTId.Data);
                //if the request user is a system admin and the request tenant is not Master
                if (roles.Contains(RolesEnum.SYSTEMADMIN.GetEnumText())/* && insTdetails.Code.ToLower() != "master"*/
                    )
                {
                    var getUserRoles = await _context.SystemAdminOtherTenantsRole
                   .Include(x => x.ApplicationRole)
                   .Where(x => x.UserId == user.Id)
                   .Select(x => new SystemAdminOtherTenantsRoleResponse
                   {
                       RoleName = x.ApplicationRole.Name,
                       InstitutionId = x.InstitutionId,
                       UserId = x.UserId,
                       RoleId = x.RoleId
                   }).ToListAsync();
                    roles = getUserRoles.Select(x => x.RoleName).ToList();
                    userRoles.AddRange(roles);
                }
                userRoles = userRoles.Select(x => x.ToLower()).Distinct().ToList();

                var (firstName, lastName, fullName) = await UpdateFirstAndLastName(new BuildNameModel
                {
                    Email = user.Email,
                    FullName = user.FullName,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                }, InsTId.Data);

                user.FullName = string.IsNullOrEmpty(user.FullName) ? fullName : user.FullName;

                // generate a new refresh token
                var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                var _config = builder.Build();
                var refreshToken = CreateRefreshToken(_config["JWT:ClientId"], Guid.Parse(user.Id));
                await _userMgmtService.TokenCreateModel(Guid.Parse(user.Id), refreshToken);

                var accessToken = await CreateAccessToken(user, user.FullName, userRoles.ToArray());
                //var NewOtp = await GenerateNewOTP(user.Id);
                var returnDto = new ReturnTokenModel
                {
                    AccessToken = accessToken.token,
                    RefreshToken = refreshToken.Value,
                    Is2FAEnabled = false,
                    Expires = accessToken.expires,
                    UserDetails = new UserResponseDto
                    {
                        Id = user.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = user.Email,
                        Fullname = user.FullName,
                        EmailConfirmed = user.EmailConfirmed,
                        IsActive = user.IsActive.Value,
                        RequiresPasswordChange = user.RequiresPasswordChange,
                        //otp = user.OTP,
                        Role = userRoles.ToList(),
                    }
                };
                //await AddToAudit(user, user.InstitutionId);
                return CustomResult<ReturnTokenModel>.Success(returnDto);
            }
            catch (Exception ex)
            {

                var exData = ex.Message;
                var msg = ex.InnerException;

                _logger.LogInformation($"Error while generating token: Email: {model.Email}, Messages: {exData}:{msg}");
                return CustomResult<ReturnTokenModel>.Failure(CustomError.UnableToGetToken, ResponseCodes.UnableToGetToken);
            }
        }
        public async Task<CustomResult<SendOTPResponseDto>> ResendLoginAsync(string userid)
        {
            try
            {
                var InsTId = await _userMgmtService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<SendOTPResponseDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                if (string.IsNullOrEmpty(userid))
                    return CustomResult<SendOTPResponseDto>.ErrorOccured("UserId is required!", ResponseCodes.BadRequestErrorCode);

                var user = await _userManager.FindByIdAsync(userid);
                if (user == null)
                    return CustomResult<SendOTPResponseDto>.ErrorOccured("Invalid email address!", ResponseCodes.BadRequestErrorCode);
                //check if the user requires to change password
                if (user.RequiresPasswordChange)
                    return CustomResult<SendOTPResponseDto>.Failure(CustomError.RequiresPasswordChange, ResponseCodes.RequiresPasswordChange);


                var NewOtp = await GenerateNewOTP(user.Id);

                return CustomResult<SendOTPResponseDto>.Success(NewOtp.Data);
            }
            catch (Exception ex)
            {

                var exData = ex.Message;
                var msg = ex.InnerException;

                _logger.LogInformation($"Error while generating OTP: userid: {userid}, Messages: {exData}:{msg}");
                return CustomResult<SendOTPResponseDto>.Failure(CustomError.UnableToGetToken, ResponseCodes.UnableToGetToken);
            }
        }

        public async Task<CustomResult<ReturnTokenModel>> RefreshToken(RefreshTokenDto model)
        {
            try
            {
                var InsTId = await _userMgmtService.GetTenantId();
                if (!InsTId.IsSuccess) return CustomResult<ReturnTokenModel>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == model.Email.ToLower() /*&& x.InstitutionId == InsTId.Data*/);
                if (user == null) return CustomResult<ReturnTokenModel>.ErrorOccured("Please Check the Login Credentials - Invalid Email/Password was entered!", ResponseCodes.BadRequestErrorCode);

                if (!user.IsActive.Value)
                    return CustomResult<ReturnTokenModel>.ErrorOccured("Account has not been activated", ResponseCodes.RequiresPasswordChange);
                var fullname = user.LastName + " " + user.FirstName;
                // generate a new refresh token
                var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                var _config = builder.Build();

                var validateToken = await _userMgmtService.ValidateRefreshToken(_config["JWT:ClientId"], model.RefreshToken);
                if (validateToken == null)
                {
                    return CustomResult<ReturnTokenModel>.ErrorOccured("Invalid refresh token!", ResponseCodes.BadRequestErrorCode);
                }
                var roles = await _userManager.GetRolesAsync(user);
                var newRefreshToken = CreateRefreshToken(_config["JWT:ClientId"], Guid.Parse(user.Id));

                await _userMgmtService.TokenCreateModel(Guid.Parse(user.Id), newRefreshToken);
                var newAccessToken = await CreateAccessToken(user, fullname, roles.ToArray());
                //await AddToAudit(user, user.InstitutionId);
                var returnDto = new ReturnTokenModel
                {
                    AccessToken = newAccessToken.token,
                    RefreshToken = newRefreshToken.Value,
                    Expires = newAccessToken.expires,
                    UserDetails = new UserResponseDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        EmailConfirmed = user.EmailConfirmed,
                        IsActive = user.IsActive.Value,
                        RequiresPasswordChange = user.RequiresPasswordChange,
                        //otp = user.OTP,
                        Role = roles.ToList(),
                    }
                };
                return CustomResult<ReturnTokenModel>.Success(returnDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ReturnTokenModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }


        private Task<(string token, DateTime expires)> CreateAccessToken(ApplicationUser user, string fullname, string[] roles)
        {
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            var _config = builder.Build();

            var tokenOptionsSection = _config.GetSection("JWT");

            var issuers = tokenOptionsSection.GetSection("Issuers").Get<string[]>();
            var audiences = tokenOptionsSection.GetSection("Audiences").Get<string[]>();

            var issuer = issuers?.FirstOrDefault() ?? throw new Exception("No Issuers configured in JWT");
            var audience = audiences?.FirstOrDefault() ?? throw new Exception("No Audiences configured in JWT");

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenOptionsSection["Secret"]));
            var tokenHandler = new JwtSecurityTokenHandler();

            var expiry = DateTime.UtcNow.AddMinutes(Convert.ToDouble(tokenOptionsSection["AccessTokenExpiration"]));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, fullname),
                    new Claim("user_id",user.Id.ToString()),
                    new Claim("LoggedOn", DateTime.UtcNow.ToString())
                }.Concat(roles.Select(role => new Claim(ClaimTypes.Role, role)))),
                Expires = expiry,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                Issuer = issuer,
                Audience = audience
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Task.FromResult((tokenHandler.WriteToken(token), expiry));
        }

        private async Task AddToAudit(ApplicationUser user, long institutionId)
        {
            string IpAddress = string.Empty;
            try
            {
                IPHostEntry heserver = Dns.GetHostEntry(Dns.GetHostName());
                IpAddress = heserver.AddressList.FirstOrDefault(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve IP address.");
                IpAddress = _httpContextAccessor.HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            }
            //await _auditLogSvc.LogToAudit(new AuditLogWriteModel
            //{
            //    CreatedAt = DateTime.UtcNow,
            //    EventType = Convert.ToInt32(AuditActionType.Create),
            //    IPAddress = IpAddress,
            //    UserId = user.Email,
            //    InstitutionId = institutionId,
            //    Action = "Login",
            //    Description = $"User [{user.Email}] logged in at {DateTime.UtcNow} from IP {IpAddress}"
            //});
            await _userMgmtService.UpdateUserLastLoginDate(user.Id);
        }

        private RefreshToken CreateRefreshToken(string clientId, Guid userId)
        {

            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            var _config = builder.Build();

            var tokenOptionsSection = _config.GetSection("JWT");
            var expiry = DateTime.UtcNow.AddMinutes(Convert.ToDouble(tokenOptionsSection["RefreshTokenExpiration"]));

            return new RefreshToken()
            {
                ClientId = clientId,
                UserId = userId,
                Value = Guid.NewGuid().ToString("N"),
                CreatedDate = DateTime.UtcNow,
                ExpiryTime = expiry
            };
        }

        public async Task<CustomResult<bool>> ChangePasswordAsync(string email, string currentPassword, string newPassword)
        {
            try
            {
                var InsTId = await _userMgmtService.GetTenantId();
                if (!InsTId.IsSuccess) return CustomResult<bool>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower() /*&& x.InstitutionId == InsTId.Data*/);
                if (user == null) return CustomResult<bool>.ErrorOccured($"User with email: {email} not found", ResponseCodes.BadRequestErrorCode);

                if (!await _userManager.CheckPasswordAsync(user, currentPassword))
                    return CustomResult<bool>.ErrorOccured("Current password is incorrect!", ResponseCodes.BadRequestErrorCode);

                var change = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

                if (change.Succeeded)
                {
                    user.RequiresPasswordChange = false;
                    user.EmailConfirmed = true;
                    user.LockoutEnabled = false;

                    user.IsActive = true;
                    _context.Users.Update(user);
                    _context.SaveChanges();

                    return CustomResult<bool>.Success(true, ResponseMessages.PasswordchangedSuccessfully);
                }
                else
                {
                    return CustomResult<bool>.ErrorOccured(false, change.Errors.FirstOrDefault().Description, ResponseCodes.OperationError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(AuthService).Name, nameof(AssignUserToRole));
                return CustomResult<bool>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
            }
        }

        public async Task<string> GetRoleIdbyRoleName(string roleName)
        {
            var user = await _roleManager.FindByNameAsync(roleName);
            return user.Id;
        }




        public async Task<CustomResult<string>> SendNewLogInPasswordBulk(DefaultPasswordEmailsListDto requests)
        {
            foreach (var email in requests.EmailsAddresses)
            {
                var sendDefault = await SendNewLogInPassword(email);
            }

            return CustomResult<string>.Success("Admin reset password, default password email sent to the user");
        }

        public async Task<CustomResult<string>> SendNewLogInPassword(string email)
        {
            try
            {
                var InsTId = await _userMgmtService.GetTenantId();
                if (!InsTId.IsSuccess) return CustomResult<string>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower()/* && x.InstitutionId == InsTId.Data*/);
                if (user == null) return CustomResult<string>.ErrorOccured($"User with email: {email} not found", ResponseCodes.BadRequestErrorCode);

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                user.ActivationToken = token;
                user.RequiresPasswordChange = false;
                user.IsDeleted = false;
                user.LockoutEnabled = false;
                user.EmailConfirmed = true;
                user.IsActive = true;
                _context.Update(user);
                _context.SaveChanges();

                var getUserRole = await _context.UserRoles.FirstOrDefaultAsync(i => i.UserId == user.Id);
                //var getInst = await _context.Institutions.FirstOrDefaultAsync(i => i.Id == getUserRole.InstitutionId);


                //var NewPassword = user.LastName.ToLower().Trim().Count() > 11
                //    || user.LastName.ToLower().Trim().Count() < 6 ? "DefPaswrd@123" : $"{user.LastName.ToLower().Trim()}@123";

                var NewPassword = "Password@123";
                var resetPasswordResp = await ResetPasswordAsync(email, token, NewPassword);
                _logger.LogInformation($"Admin reset password: Response After Email Notification on Password reset: {JsonConvert.SerializeObject(resetPasswordResp)}");


                if (resetPasswordResp.IsSuccess)
                {
                    _logger.LogInformation($"New Default Password Sent: Email {user.Email} and New default password: {NewPassword} at {DateTime.Now}");

                    await _emailService.SendNewDefaultPasswordEmailAsync(new SendWelcomeEmailVM
                    {
                        //InstitutionBaseUrl = $"{getInst.HostName}/auth/login",
                        Fullname = $"{user.LastName} {user.FirstName}",
                        Password = NewPassword,
                        receiverEmail = email,
                    });

                    return CustomResult<string>.Success("Admin reset password, default password email sent to the user");
                }

                return CustomResult<string>.ErrorOccured(resetPasswordResp.Message, ResponseCodes.OperationError);
                //return CustomResult<string>.ErrorOccured(CustomError.SystemExceptionError, ResponseCodes.OperationError);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Admin reset password: Error After Email Notification on Password reset: {JsonConvert.SerializeObject(ex)}");
                _logger.LogError(ex, "Admin reset password: An error occurred in {Repo}.{MethodName}", typeof(AuthService).Name, nameof(AssignUserToRole));
                return CustomResult<string>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
            }
        }



        public async Task<CustomResult<string>> ForgotPasswordAsync(string email)
        {
            try
            {
                var InsTId = await _userMgmtService.GetTenantId();
                if (!InsTId.IsSuccess) return CustomResult<string>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower() /*&& x.InstitutionId == InsTId.Data*/);
                if (user == null) return CustomResult<string>.ErrorOccured($"User with email: {email} not found", ResponseCodes.BadRequestErrorCode);

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                user.ActivationToken = token;
                var activationTokenexpiresOn = DateTime.UtcNow.AddMinutes(1440);
                user.ActivationTokenExpiresOn = activationTokenexpiresOn;
                user.IsTokenActive = true;
                _context.Update(user);
                _context.SaveChanges();

                var getUserRole = await _context.UserRoles.FirstOrDefaultAsync(i => i.UserId == user.Id);
                //var getInst = await _context.Institutions.FirstOrDefaultAsync(i => i.Id == getUserRole.InstitutionId);

                var res = await _emailService.SendPasswordResetEmailAsync(new SendPasswordResetEmailVM
                {
                    PasswordResetToken = token,
                    //InstitutionBaseUrl = getInst.HostName,
                    Fullname = $"{user.LastName} {user.FirstName}",
                    receiverEmail = email,
                    ExpiresOn = activationTokenexpiresOn
                });

                _logger.LogInformation($"After Email Notification on Password reset: {JsonConvert.SerializeObject(res)}");

                return CustomResult<string>.Success(res.IsSuccess ? "Password Token link Sent" : ResponseMessages.SystemExceptionErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error After Email Notification on Password reset: {JsonConvert.SerializeObject(ex)}");
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(AuthService).Name, nameof(AssignUserToRole));
                return CustomResult<string>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
            }
        }
        public async Task<CustomResult<string>> ResetPasswordAsync(string email, string token, string newPassword)
        {
            try
            {
                var InsTId = await _userMgmtService.GetTenantId();
                if (!InsTId.IsSuccess) return CustomResult<string>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower() /*&& x.InstitutionId == InsTId.Data*/);
                if (user == null) return CustomResult<string>.ErrorOccured($"User with email: {email} not found", ResponseCodes.BadRequestErrorCode);

                //check if the activation token is expired
                if (user.ActivationTokenExpiresOn < DateTime.UtcNow) return CustomResult<string>.ErrorOccured($"Reset Token is expired, generate another one", ResponseCodes.BadRequestErrorCode);


                //if(!user.IsTokenActive) return CustomResult<string>.ErrorOccured($"Reset token has expired!", ResponseCodes.BadRequestErrorCode);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);




                //user.ActivationToken = null;
                user.IsActive = true;
                user.IsDeleted = false;
                user.RequiresPasswordChange = false;
                user.LockoutEnabled = false;
                user.IsTokenActive = false;
                _context.Update(user);
                _context.SaveChanges();

                //return CustomResult<string>.Success(result.Succeeded ? "Password reset successful" : string.Join("; ", result.Errors.Select(e => e.Description)), ResponseMessages.UserRoleAssignedSuccessfull);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"response from resettung password is successed: {result.Succeeded}");
                    return CustomResult<string>.Success("Password reset successful");
                    //string.Join("; ", result.Errors.Select(e => e.Description)), ResponseMessages.PasswordResetSuccessfully
                }
                else
                {
                    _logger.LogInformation($"response from resetting password is not successed:" +
                        $" {string.Join("; ", result.Errors.Select(e => e.Description))}");
                    return CustomResult<string>.Success($" {string.Join("; ", result.Errors.Select(e => e.Description))}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(AuthService).Name, nameof(AssignUserToRole));
                return CustomResult<string>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
            }
        }

        public async Task ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            await _userManager.ConfirmEmailAsync(user, token);
        }

        public async Task<CustomResult<bool>> AssignUserToRole(string userId, string rolename)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with Id: {userId} not found");
                    return CustomResult<bool>.Failure(CustomError.UserRoleNotFoundError, ResponseCodes.RoleNotFoundErrorCode);
                }
                if (!await _userManager.IsInRoleAsync(user, rolename))
                {
                    await _userManager.AddToRoleAsync(user, rolename);
                }

                return CustomResult<bool>.Success(true, ResponseMessages.UserRoleAssignedSuccessfull);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(AuthService).Name, nameof(AssignUserToRole));
                return CustomResult<bool>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
            }
        }

        public async Task<CustomResult<RegisterUserResponseDto>> RegisterAsync(RegisterUserRequestDto model, long institutionId)
        {
            try
            {
                var InsTId = await _userMgmtService.GetTenantId();
                if (!InsTId.IsSuccess) return CustomResult<RegisterUserResponseDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                if (await _context.Users.AnyAsync(x => x.Email.ToUpper() == model.Email.ToUpper() /*&& x.InstitutionId == InsTId.Data*/))
                    return CustomResult<RegisterUserResponseDto>.Failure(CustomError.AccountAlreadyExistsError, ResponseCodes.UnableToProfileUser);

                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName };

                user.Id = Guid.NewGuid().ToString("N");
                user.IsActive = model.IsActive;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.FullName = model.FullName;
                //user.Gender = model.Gender;

                user.EmailConfirmed = true;
                //user.RequiresPasswordChange = true;
                user.RequiresPasswordChange = false;
                //user.InstitutionId = institutionId;
                var result = await _userManager.CreateAsync(user, model.Password);

                var getRole = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == model.RoleId);
                if (getRole == null)
                {
                    _logger.LogWarning("Role with id {RoleId} not found", model.RoleId);
                    return CustomResult<RegisterUserResponseDto>.Failure(CustomError.UserRoleNotFoundError, ResponseCodes.RoleNotFoundErrorCode);
                }

                if (!result.Succeeded)
                    return CustomResult<RegisterUserResponseDto>.Failure(CustomError.UnableToProfileUser, ResponseCodes.UnableToProfileUser);

                return CustomResult<RegisterUserResponseDto>.Success(
                    new RegisterUserResponseDto
                    {
                        Message = ResponseMessages.UserCreatedSuccessfully,
                        Status = true,
                        UserId = user.Id

                    }, ResponseMessages.UserCreatedSuccessfully
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(AuthService).Name, nameof(RegisterAsync));
                return CustomResult<RegisterUserResponseDto>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
            }
        }

        public async Task<CustomResult<RegisterUserResponseDto>> UpdateUserAsync(string userid, UpdateUserRequestDto model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var userdetails = await _context.Users.FirstOrDefaultAsync(x => x.Id == userid);

                    if (userdetails is null)
                    {
                        return CustomResult<RegisterUserResponseDto>.Failure(CustomError.UserRoleNotFoundError
                            , ResponseCodes.NotFoundErrorCode);
                    }

                    if (model.PassportUpload is not null && model.PassportUpload.Length > 0)
                    {
                        //var logoResult = await UploadFileAsync(model.PassportUpload);
                        //if (!logoResult.Success)
                        //    return CustomResult<RegisterUserResponseDto>.ErrorOccured(logoResult.Error, ResponseCodes.BadRequestErrorCode);

                        //userdetails.ProfilePixUrl = logoResult.Url;
                    }

                    userdetails.FirstName = model.FirstName;
                    userdetails.FullName = $"{model.FirstName}  {model.LastName}";
                    //userdetails.Gender = model.Gender;
                    userdetails.PhoneNumber = model.PhoneNumber;
                    userdetails.IsActive = model.IsActive;

                    _context.Users.Update(userdetails);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CustomResult<RegisterUserResponseDto>.Success(
                        new RegisterUserResponseDto
                        {
                            Message = ResponseMessages.UserUpdatedSuccessfully,
                            Status = true,
                            UserId = userid

                        }, ResponseMessages.UserUpdatedSuccessfully);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(AuthService).Name, nameof(RegisterAsync));
                    return CustomResult<RegisterUserResponseDto>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
                }
            });
        }


        public async Task<CustomResult<RegisterUserResponseDto>> RegisterEndUsersAsync(RegisterANewUserRequestDto model)
        {
            var InsTId = await _userMgmtService.GetTenantId();
            var loggedInUserclaim = await _userMgmtService.GetUserClaim();
            if (!InsTId.IsSuccess)
            {
                return CustomResult<RegisterUserResponseDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                //if (!await _context.Institutions.AnyAsync(x => x.Id == InsTId.Data))
                //    return CustomResult<RegisterUserResponseDto>.ErrorOccured("Institution does not exists", ResponseCodes.NotFoundErrorCode);
                try
                {
                    //var Inst = await _context.Institutions.FirstOrDefaultAsync(i => i.Id == InsTId.Data);

                    //if (Inst == null)
                    //{
                    //    return CustomResult<RegisterUserResponseDto>.ErrorOccured("Institution not found!", ResponseCodes.NotFoundErrorCode);
                    //}


                    //if (await _context.Users.AnyAsync(x => x.Email.ToUpper() == model.Email.ToUpper() /*&& x.InstitutionId == InsTId.Data*/))
                    //    return CustomResult<RegisterUserResponseDto>.Failure(CustomError.AccountAlreadyExistsError, ResponseCodes.UnableToProfileUser);

                    var getRrole = await _context.Roles.FirstOrDefaultAsync(x => x.Name.ToLower() == "enduser");

                    var NewPassword = "Password@123";

                    var registerUser = await RegisterAsync(new RegisterUserRequestDto
                    {
                        Email = model.Email,
                        FullName = $"{model.LastName} {model.FirstName}",
                        Password = model.Password,
                        //Password = NewPassword,
                        //PhoneNumber = model.PhoneNumber,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        RoleId = getRrole.Id,
                        //RoleId = model.RoleId,
                        IsActive = true,
                        //Gender = model.Gender
                    }, InsTId.Data);

                    if (!registerUser.IsSuccess)
                    {
                        return registerUser;
                        //return CustomResult<RegisterUserResponseDto>.Failure(CustomError.UnableToProfileUser, ResponseCodes.UnableToProfileUser);
                    }

                    var addUserAsSystemAdmin = await _context.UserRoles.AddAsync(new ApplicationUserRole
                    {
                        CreatedAt = DateTime.UtcNow,
                        UserId = registerUser.Data.UserId,
                        RoleId = getRrole.Id,
                        //InstitutionId = InsTId.Data
                    });

                    //if the new profile is a system admin role, 
                    var getRole = await _context.Roles.FirstOrDefaultAsync(x => x.Id == getRrole.Id);

                    //if (getRole.Name.ToUpper() == RolenamesConstant.SYSTEMADMIN)
                    //{
                        //check if the logged in user is a system admin

                        //if (loggedInUserclaim.Role.FirstOrDefault(x => x.ToLower().Contains(RolenamesConstant.SYSTEMADMIN.ToLower())) == null)
                        //{
                        //    return CustomResult<RegisterUserResponseDto>.Failure(CustomError.YourRoleIsNotAuthorisedforThisAction, ResponseCodes.YourRoleIsNotAuthorisedforThisAction);

                        //}


                        //var getInsAdminRole = await _context.Roles.FirstOrDefaultAsync(x => x.Name.ToLower() == RolenamesConstant.INSTITUTIONADMIN.ToLower());
                        //var getAllInst = await _context.Institutions.Select(x => x.Id).ToListAsync();

                        //foreach (var inst in getAllInst)
                        //{
                        //    await _context.SystemAdminOtherTenantsRole.AddAsync(new SystemAdminOtherTenantsRole
                        //    {
                        //        CreatedAt = DateTime.UtcNow,
                        //        InstitutionId = inst,
                        //        RoleId = getInsAdminRole.Id,
                        //        UserId = registerUser.Data.UserId,
                        //    });
                        //}
                    //}

                    //if (getRole.Name.ToUpper() == RolenamesConstant.TUTOR)
                    //{
                    //    await _context.Tutors.AddAsync(new Tutor
                    //    {
                    //        CreatedAt = DateTime.UtcNow,
                    //        InstitutionId = InsTId.Data,
                    //        TutorName = $"{model.LastName} {model.FirstName}",
                    //        ApplicationUserId = registerUser.Data.UserId,
                    //        IsActive = model.IsActive,
                    //        IsDeleted = false
                    //    });
                    //}

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"new User to create with email: {model.Email}, Default Password: {NewPassword}");
                    //_ = Task.Run(async () =>
                    //{
                        //await _emailService.SendWelcomeEmailAsync(new SendWelcomeEmailVM
                        //{
                        //    //InstitutionBaseUrl = $"{Inst.HostName}/auth/login",
                        //    Fullname = $"{model.LastName} {model.FirstName}",
                        //    Password = NewPassword,
                        //    receiverEmail = model.Email,
                        //});
                        await _emailService.SendSingleWelcomeToQGuardEmailAsync($"{model.LastName} {model.FirstName}", model.Email, NewPassword);

                    //});
                    return registerUser;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(AuthService).Name, nameof(RegisterAsync));
                    return CustomResult<RegisterUserResponseDto>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
                }
            });
        }
        public async Task<CustomResult<RegisterUserResponseDto>> RegisterOtherUsersAsync(RegisterOtherUserRequestDto model)
        {
            var InsTId = await _userMgmtService.GetTenantId();
            var loggedInUserclaim = await _userMgmtService.GetUserClaim();
            if (!InsTId.IsSuccess)
            {
                return CustomResult<RegisterUserResponseDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                //if (!await _context.Institutions.AnyAsync(x => x.Id == InsTId.Data))
                //    return CustomResult<RegisterUserResponseDto>.ErrorOccured("Institution does not exists", ResponseCodes.NotFoundErrorCode);
                try
                {
                    //var Inst = await _context.Institutions.FirstOrDefaultAsync(i => i.Id == InsTId.Data);

                    //if (Inst == null)
                    //{
                    //    return CustomResult<RegisterUserResponseDto>.ErrorOccured("Institution not found!", ResponseCodes.NotFoundErrorCode);
                    //}
                    var NewPassword = "Password@123";

                    var registerUser = await RegisterAsync(new RegisterUserRequestDto
                    {
                        Email = model.Email,
                        FullName = $"{model.LastName} {model.FirstName}",
                        Password = NewPassword,
                        PhoneNumber = model.PhoneNumber,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        RoleId = model.RoleId,
                        IsActive = model.IsActive,
                        Gender = model.Gender
                    }, InsTId.Data);

                    if (!registerUser.IsSuccess)
                    {
                        //return CustomResult<RegisterUserResponseDto>.Failure(CustomError.UnableToProfileUser, ResponseCodes.UnableToProfileUser);
                        return registerUser;
                    }

                    var addUserAsSystemAdmin = await _context.UserRoles.AddAsync(new ApplicationUserRole
                    {
                        CreatedAt = DateTime.UtcNow,
                        UserId = registerUser.Data.UserId,
                        RoleId = model.RoleId,
                        //InstitutionId = InsTId.Data
                    });

                    //if the new profile is a system admin role, 
                    var getRole = await _context.Roles.FirstOrDefaultAsync(x => x.Id == model.RoleId);

                    if (getRole.Name.ToUpper() == RolenamesConstant.SYSTEMADMIN)
                    {
                        //check if the logged in user is a system admin

                        if (loggedInUserclaim.Role.FirstOrDefault(x => x.ToLower().Contains(RolenamesConstant.SYSTEMADMIN.ToLower())) == null)
                        {
                            return CustomResult<RegisterUserResponseDto>.Failure(CustomError.YourRoleIsNotAuthorisedforThisAction, ResponseCodes.YourRoleIsNotAuthorisedforThisAction);

                        }


                        var getInsAdminRole = await _context.Roles.FirstOrDefaultAsync(x => x.Name.ToLower() == RolenamesConstant.INSTITUTIONADMIN.ToLower());
                        //var getAllInst = await _context.Institutions.Select(x => x.Id).ToListAsync();

                        //foreach (var inst in getAllInst)
                        //{
                        //    await _context.SystemAdminOtherTenantsRole.AddAsync(new SystemAdminOtherTenantsRole
                        //    {
                        //        CreatedAt = DateTime.UtcNow,
                        //        InstitutionId = inst,
                        //        RoleId = getInsAdminRole.Id,
                        //        UserId = registerUser.Data.UserId,
                        //    });
                        //}
                    }

                    //if (getRole.Name.ToUpper() == RolenamesConstant.TUTOR)
                    //{
                    //    await _context.Tutors.AddAsync(new Tutor
                    //    {
                    //        CreatedAt = DateTime.UtcNow,
                    //        InstitutionId = InsTId.Data,
                    //        TutorName = $"{model.LastName} {model.FirstName}",
                    //        ApplicationUserId = registerUser.Data.UserId,
                    //        IsActive = model.IsActive,
                    //        IsDeleted = false
                    //    });
                    //}

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"new User to create with email: {model.Email}, Default Password: {NewPassword}");
                    _ = Task.Run(async () =>
                    {
                        await _emailService.SendWelcomeEmailAsync(new SendWelcomeEmailVM
                        {
                            //InstitutionBaseUrl = $"{Inst.HostName}/auth/login",
                            Fullname = $"{model.LastName} {model.FirstName}",
                            Password = NewPassword,
                            receiverEmail = model.Email,
                        });
                    });
                    return registerUser;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(AuthService).Name, nameof(RegisterAsync));
                    return CustomResult<RegisterUserResponseDto>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
                }
            });
        }

        public async Task<CustomResult<bool>> ValidateOtp(string userId, string Otp)
        {
            try
            {
                var getUserDetails = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

                //if (getUserDetails.OTP == Otp)
                //{

                //    getUserDetails.OTP = string.Empty;
                //    getUserDetails.RequiresPasswordChange = false;

                //    _context.Users.Update(getUserDetails);
                //    _context.SaveChanges();

                return CustomResult<bool>.Success(
                true, ResponseMessages.OTPValidatedSuccessfully);

                //}
                //return CustomResult<bool>.Failure(CustomError.InvalidOTP, ResponseCodes.InvalidOTP);

            }
            catch (Exception ex)
            {

                var exData = ex.Message;
                var msg = ex.InnerException;

                _logger.LogInformation($"Error while validating OTP: OTP: {Otp} by user: {userId}, Messages: {exData}:{msg}");
                return CustomResult<bool>.Failure(CustomError.InvalidOTP, ResponseCodes.InvalidOTP);

            }
        }

        public async Task<(string fName, string lName, string fullName)> UpdateFirstAndLastName(BuildNameModel model, long tenantId)
        {
            if (string.IsNullOrEmpty(model.LastName) || string.IsNullOrEmpty(model.FirstName))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower()/* && u.InstitutionId == tenantId*/);
                if (user != null)
                {
                    var (first, last) = Helper.SplitFullName(model.FullName);

                    user.FirstName = first;
                    user.LastName = string.IsNullOrEmpty(last) ? "NA" : last;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();

                    return (first, last, $"{user.FirstName} {user.LastName}");
                }
                return ("NA", "NA", "NA");
            }
            var fullname = string.IsNullOrEmpty(model.FullName) ? $"{model.FirstName} {model.LastName}" : model.FullName;

            return (model.FirstName, model.LastName, fullname);
        }

        public async Task<CustomResult<SendOTPResponseDto>> GenerateNewOTP(string userId)
        {
            try
            {
                var getUserDetails = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

                //generate new OTP and update the user OTP
                var newOTP = qguardbackend.Data.Common.Utility.GenerateOTP(5);

                //getUserDetails.OTP = newOTP;

                _context.Users.Update(getUserDetails);
                _context.SaveChanges();
                //send the OTP to user Email

                var sendEmail = await _emailService.SendOTPEmailAsync(new SendOTPVerificationEmailVM
                {
                    Fullname = $"{getUserDetails.LastName} {getUserDetails.FirstName}",
                    OTP = newOTP,
                    receiverEmail = getUserDetails.Email,
                });

                if (sendEmail.IsSuccess)
                //if (true)
                {
                    return CustomResult<SendOTPResponseDto>.Success(
                    new SendOTPResponseDto
                    {
                        UserId = userId,
                        OTP = newOTP,

                    }, ResponseMessages.OTPGeneratedAndSentSuccessfully);
                }
                return CustomResult<SendOTPResponseDto>.Failure(CustomError.InvalidOTP, ResponseCodes.InvalidOTP);
            }
            catch (Exception ex)
            {
                var exData = ex.Message;
                var msg = ex.InnerException;

                _logger.LogInformation($"Error while generating OTP, by user: {userId}, Messages: {exData}:{msg}");
                return CustomResult<SendOTPResponseDto>.Failure(CustomError.InvalidOTP, ResponseCodes.InvalidOTP);
            }
        }
    }

    public class DefaultPasswordEmailsListDto
    {
        public List<string> EmailsAddresses { get; set; }

    }
}