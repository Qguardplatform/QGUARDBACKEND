using AutoMapper;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums.Constants;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Model;
using qguardbackend.Shared.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace qguardbackend.Core.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserManagementService> _logger;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserManagementService(AppDbContext context,
            RoleManager<ApplicationRole> roleManager, IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration config,
            ILogger<UserManagementService> logger)
        {
            _context = context;
            _logger = logger; _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CustomResult<ApplicationUserResponse>> GetUserDetailsByIdAsync(string userId)
        {
            var tempQuery = await _context.Users
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();

            var result = new List<ApplicationUserResponse>();

            var roleNames = await _userManager.GetRolesAsync(tempQuery);
            var roles = new List<ApplicationUserRoleResponse>();

            foreach (var roleName in roleNames)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    roles.Add(new ApplicationUserRoleResponse
                    {
                        Id = Guid.Parse(role.Id),
                        Rolename = role.Name
                    });
                }
            }

            //var getIns = _context.Institutions.Where(x => x.Id == tempQuery.InstitutionId).FirstOrDefault();

            //var Inst = _mapper.Map<InstitutionResponseDto>(getIns);

            var userDetails = _mapper.Map<ApplicationUserResponse>(tempQuery);

            userDetails.RoleId = roles.FirstOrDefault()?.Id ?? Guid.Empty;
            userDetails.Roles = roles;
            //userDetails.Institution = Inst;
            userDetails.Firstname = tempQuery.FirstName;
            userDetails.Lastname = tempQuery.LastName;
            userDetails.Gender = tempQuery.Gender;
            //userDetails.Institution = Inst;
            userDetails.Address = $"{tempQuery.Street}, {tempQuery.City}, {tempQuery.State}, {tempQuery.ZipCode}, {tempQuery.Country}";

            return CustomResult<ApplicationUserResponse>.Success(userDetails);
        }


        public async Task<CustomResult<ApplicationUserResponse>> GetLoggedInUser()
        {
            try
            {
                var userClaims = await GetUserClaim();

                if (userClaims == null || string.IsNullOrWhiteSpace(userClaims?.Email))
                {
                    _logger.LogError("Invalid user claims.... user claims null");
                    return CustomResult<ApplicationUserResponse>.Failure(CustomError.UserClaimsError, ResponseCodes.InvalidUserClaims);
                }


                var userData = await GetUserDetailsByIdAsync(userClaims.UserId); ;

                if (userData.Data == null)
                {
                    _logger.LogError("User with email address {@email} does not exist", userClaims.Email);
                    return CustomResult<ApplicationUserResponse>.Failure(CustomError.UnableToRetrieveUserProfile, ResponseCodes.NotFoundErrorCode);
                }

                //check if the logged in user is profiled to access the institution on the header

                _logger.LogInformation("Logged in user info: {@info}", userData.Data);
                return CustomResult<ApplicationUserResponse>.Success(userData.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(UserManagementService).Name, nameof(GetLoggedInUser));
                throw;
            }
        }

        public async Task<CustomResult<PagedList<ApplicationUserResponse>>> GetUsersWithRolesAsync(UsersFilterModel query)
        {

            var tempQuery = _userManager.Users
                //.Include(u => u.Institution)
                .OrderByDescending(x => x.CreatedAt)
                .AsQueryable();

            if (query.StartDate.HasValue && query.EndDate.HasValue)
            {
                tempQuery = tempQuery
                   .Where(x => x.CreatedAt >= query.StartDate.Value && x.CreatedAt <= query.EndDate.Value);
            }
            else if (query.StartDate.HasValue && !query.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.CreatedAt >= query.StartDate.Value);
            }
            else if (!query.StartDate.HasValue && query.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.CreatedAt <= query.EndDate.Value);
            }

            //var tenCode = await GetTenantCode();
            //if (!string.IsNullOrEmpty(tenCode.Data) && tenCode.Data.ToLower() != "master")
            //{
            //    tempQuery = tempQuery.Where(x => x.Institution.Code.ToLower() ==
            //    tenCode.Data.ToLower());
            //}

            if (query != null && !string.IsNullOrEmpty(query.SearchWord))
            {
                var keyWord = query.SearchWord.Trim().ToLower();
                tempQuery = tempQuery.Where(x => x.Id.ToString().ToLower().Contains(keyWord.ToLower())
                || x.Id.ToString().ToLower().Contains(keyWord.ToLower())
                || x.FirstName.ToString().ToLower().Contains(keyWord.ToLower())
                || x.LastName.ToString().ToLower().Contains(keyWord.ToLower()));
            }

            if (query.IsActive.HasValue)
            {
                bool status = query.IsActive.Value;
                tempQuery = tempQuery.Where(x => x.IsActive == status);
            }

            if (query.Role.HasValue)
            {
                var getRoleId = await _context.Roles.FirstOrDefaultAsync(x => x.Name.ToLower() == query.Role.Value.ToString().ToLower());

                var AllUsersInRole = await _context.UserRoles.Where(x => x.RoleId == getRoleId.Id).ToListAsync();
                var theUsersId = AllUsersInRole.Select(x => x.UserId).ToList();

                tempQuery = tempQuery.Where(t => theUsersId.Contains(t.Id));
            }
            //int count = result.Count();
            int count = tempQuery.Count();
            //return result;
            var paginatedData = await tempQuery.OrderByDescending(x => x.CreatedAt)
                                .Paginate(query.PageNumber.Value, query.PageSize.Value).ToListAsync();

            var result = new List<ApplicationUserResponse>();

            foreach (var user in paginatedData)
            {
                var roleNames = await _userManager.GetRolesAsync(user);
                var roles = new List<ApplicationUserRoleResponse>();

                foreach (var roleName in roleNames)
                {
                    var role = await _roleManager.FindByNameAsync(roleName);
                    if (role != null)
                    {
                        roles.Add(new ApplicationUserRoleResponse
                        {
                            Id = Guid.Parse(role.Id),
                            Rolename = role.Name
                        });
                    }
                }

                //var getIns = _context.Institutions.Where(x => x.Id == user.InstitutionId).FirstOrDefault();

                //var Inst = _mapper.Map<InstitutionResponseDto>(getIns);
                result.Add(new ApplicationUserResponse
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Street = user.Street,
                    City = user.City,
                    State = user.State,
                    Gender = user.Gender,
                    ZipCode = user.ZipCode,
                    FullName = $"{user.FirstName} {user.LastName}",
                    DisplayName = user.UserName,
                    Address = $"{user.Street}, {user.City}, {user.State}, {user.ZipCode}, {user.Country}",
                    Country = user.Country,
                    AuthProvider = user.AuthProvider,
                    ProfilePixUrl = user.ProfilePixUrl,
                    LastSignInDate = user.LastSignInDate,
                    IsActive = user.IsActive,
                    ApprovalStatus = user.ApprovalStatus,
                    ApprovalActionBy = user.ApprovalActionBy,
                    ApprovalActionDate = user.ApprovalActionDate,
                    Roles = roles,
                    //Institution = Inst,
                    RoleId = roles.FirstOrDefault()?.Id ?? Guid.Empty
                });
            }


            var pagedList = new PagedList<ApplicationUserResponse>(result, query.PageNumber.Value, query.PageSize.Value, count);
            _logger.LogInformation(" user accounts retrieved successfully. {Count} record(s) found", pagedList.TotalPageCount);
            return CustomResult<PagedList<ApplicationUserResponse>>.Success(pagedList);
        }

        public async Task<CustomResult<PagedList<ApplicationUserResponse>>> GetActiveUsersListAsync(QueryModelMini query)
        {
            var tempQuery = _userManager.Users.Where(x => x.IsActive).OrderByDescending(x => x.CreatedAt).AsQueryable();

            if (query.StartDate.HasValue && query.EndDate.HasValue)
            {
                tempQuery = tempQuery
                   .Where(x => x.CreatedAt >= query.StartDate.Value && x.CreatedAt <= query.EndDate.Value);
            }
            else if (query.StartDate.HasValue && !query.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.CreatedAt >= query.StartDate.Value);
            }
            else if (!query.StartDate.HasValue && query.EndDate.HasValue)
            {
                tempQuery = tempQuery
                    .Where(x => x.CreatedAt <= query.EndDate.Value);
            }


            if (query != null && !string.IsNullOrEmpty(query.SearchWord))
            {
                var keyWord = query.SearchWord.Trim().ToLower();
                tempQuery = tempQuery.Where(x => x.Id.ToString().ToLower().Contains(keyWord.ToLower())
                || x.Id.ToString().ToLower().Contains(keyWord.ToLower())
                || x.FirstName.ToString().ToLower().Contains(keyWord.ToLower())
                || x.LastName.ToString().ToLower().Contains(keyWord.ToLower())
                );
            }

            IQueryable<ApplicationUserResponse> entityQuery = EntitySelectSearch(tempQuery.OrderByDescending(x => x.CreatedAt));

            var paginatedData = await entityQuery.Paginate(query.PageNumber.Value, query.PageSize.Value).ToListAsync();

            int count = entityQuery.Count();
            var pagedList = new PagedList<ApplicationUserResponse>(paginatedData, query.PageNumber.Value, query.PageSize.Value, count);
            _logger.LogInformation(" user accounts retrieved successfully. {Count} record(s) found", pagedList.TotalPageCount);
            return CustomResult<PagedList<ApplicationUserResponse>>.Success(pagedList);
        }

        public async Task<CustomResult<string>> ChangeAccountStatus(string userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
                if (user == null)
                {
                    return CustomResult<string>.ErrorOccured("User not found!", ResponseCodes.NotFoundErrorCode);
                }

                user.IsActive = !user.IsActive;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                if (user.IsActive == true)
                {
                    return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Account enabled successfully");
                }
                else
                {
                    return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Account disabled successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<bool> CheckIfAccountIsActive(string id)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
                if (user == null) throw new Exception("User not found!");
                if (user.IsActive == true)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> ValidateRefreshToken(string clientId, string refreshToken)
        {
            try
            {
                // check if the received refreshToken exists for the given clientId
                var rt = await _context.RefreshToken
                                    .Where(r => r.ClientId == clientId && r.Value == refreshToken)
                                    .FirstOrDefaultAsync();
                if (rt == null)
                {
                    return null;
                }
                // check if refresh token is expired
                if (rt.ExpiryTime < DateTime.UtcNow)
                {
                    return null;
                }
                return rt.UserId.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        public async Task TokenCreateModel(Guid userId, RefreshToken token)
        {
            try
            {
                // first we delete any existing old refreshtokens
                var oldrTokens = _context.RefreshToken.Where(rt => rt.UserId == userId);

                if (oldrTokens != null)
                {
                    foreach (var oldrt in oldrTokens)
                    {
                        _context.RefreshToken.Remove(oldrt);
                    }
                }
                // Add new refresh token to Database
                _context.RefreshToken.Add(token);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task UpdateUserLastLoginDate(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null) throw new Exception("User not found!");
            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SeedDefaultUser()
        {
            if (_context.Users.Any())
            {
                return;
            }
            var role = await _context.Roles.FirstOrDefaultAsync(c => c.Name.ToLower() == RolenamesConstant.SYSTEMADMIN.ToLower());
            if (role == null) return;

            //var institution = await _context.Institutions.FirstOrDefaultAsync(c => c.Code.ToLower() == "master");
            //if (institution == null) return;

            var hasher = new PasswordHasher<ApplicationUser>();
            Guid ID = Guid.NewGuid();
            string email = "daniel.ogwu@vigilearn.com";

            var user = new ApplicationUser()
            {
                Id = ID.ToString(),
                Email = email,
                FirstName = "Super",
                LastName = "Admin",
                UserName = "Super.Admin",
                PhoneNumber = "0800000000",
                EmailConfirmed = true,
                IsDeleted = false,
                NormalizedEmail = email.ToUpper(),
                PhoneNumberConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                TwoFactorEnabled = false,
                AccountActivationDate = DateTime.UtcNow,
                //InstitutionId = institution.Id
            };
            user.PasswordHash = hasher.HashPassword(user, "Password@123");
            await _context.AddAsync(user);

            var userRole = new ApplicationUserRole()
            {
                RoleId = role.Id,
                UserId = ID.ToString(),
                CreatedAt = DateTime.UtcNow,
                //InstitutionId = institution.Id
            };
            await _context.AddAsync(userRole);
            await _context.SaveChangesAsync();

          

            //create default 

            var GeneralAccess = new SystemAdminOtherTenantsRole
            {
                //InstitutionId = institution.Id,
                IsActive = true,
                IsDeleted = false,
                RoleId = role.Id,
                UserId = ID.ToString()

            };

            await _context.AddAsync(GeneralAccess);
            await _context.SaveChangesAsync();

            //

        }

        private IQueryable<ApplicationUserResponse> EntitySelectSearch(IQueryable<ApplicationUser> query)
        {
            return query.Select(x => new ApplicationUserResponse()
            {
                UserName = x.UserName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                IsActive = x.IsActive,
                RoleId = Guid.Parse(x.UserRoles.FirstOrDefault().RoleId),
                Id = x.Id,
            });
        }

        public async Task<string> generateResetTokenAsync()
        {
            // token is a cryptographically strong random sequence of values
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

            var users = await _userManager.Users
                .Where(x => x.ActivationToken == token).ToListAsync();

            if (users != null && users.Count > 0)
                return await generateResetTokenAsync();

            return token;
        }

        public async Task<string> generateVerificationTokenAsync()
        {
            // token is a cryptographically strong random sequence of values
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

            var users = await _userManager.Users
                .Where(x => x.ActivationToken == token).ToListAsync();

            if (users != null && users.Count > 0)
                return await generateVerificationTokenAsync();

            return token;
        }

        public async Task<LoginResponse> GenerateJwtToken(string email, string applicationCode, ICollection<ApplicationRole> userRoles)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim("TenantCode", applicationCode),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            // Add roles to claims
            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole.Name));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"] ?? ""));

            var token = new JwtSecurityToken(
                issuer: _config["JWT:ValidIssuer"],
                audience: _config["JWT:ValidAudience"],

                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo,
                Message = "Token generated successfully"
            };
        }

        public async Task<string> GenerateRefreshToken(string email, string SAPId)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"] ?? ""));

            var token = new JwtSecurityToken(
                issuer: _config["JWT:ValidIssuer"],
                audience: _config["JWT:ValidAudience"],

                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<CustomResult<long>> GetTenantId()
        {

            // Ensure HttpContext exists
            if (_httpContextAccessor.HttpContext == null)
            {
                return CustomResult<long>.ErrorOccured("No Header", ResponseCodes.AlreadyExistErrorCode);

            }

            // Retrieve JWT token from Authorization header
            string tenCode = _httpContextAccessor.HttpContext.Request.Headers["TenantCode"].FirstOrDefault();

            //var getInstitutionId = await _context.Institutions.FirstOrDefaultAsync(x => x.Code.ToLower() == tenCode.ToLower());

            //if (getInstitutionId == null)
            //{
            //    return CustomResult<long>.Failure(CustomError.TenantNotFound, ResponseCodes.NotFoundErrorCode);
            //}
            //return CustomResult<long>.Success(getInstitutionId.Id);
            return CustomResult<long>.Success(1);
        }

        public async Task<CustomResult<string>> GetTenantCode()
        {

            // Ensure HttpContext exists
            if (_httpContextAccessor.HttpContext == null)
            {
                return CustomResult<string>.ErrorOccured("No Header", ResponseCodes.AlreadyExistErrorCode);

            }

            // Retrieve JWT token from Authorization header
            var tenCode = _httpContextAccessor.HttpContext.Request.Headers["TenantCode"].FirstOrDefault();

            return CustomResult<string>.Success(tenCode);
        }
        public async Task<AuthClaims> GetUserClaim()
        {
            // Ensure HttpContext exists
            if (_httpContextAccessor.HttpContext == null)
            {
                return null; // Return null for background services
            }

            // Retrieve JWT token from Authorization header
            string jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(jwtToken) || !jwtToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return null; // No valid token provided
            }

            try
            {
                jwtToken = jwtToken.Substring("Bearer ".Length).Trim();

                // Decode the JWT token
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                if (jsonToken != null)
                {
                    // Extract claims
                    var emailClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                    var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
                    var roleClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
                    var roles = _userManager.GetRolesAsync(_userManager.FindByIdAsync(userIdClaim).Result);
                    return new AuthClaims
                    {
                        Email = emailClaim,
                        Role = roles.Result.ToList(),
                        UserId = userIdClaim
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid token: {ex.Message}"); // Log error instead of returning string
            }

            return null; // Return null on failure
        }

        public void SetUserClaims(HttpContext context, string userId, string role)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role ?? string.Empty)
            }, "firebase"));
        }

        public async Task<string> GetTokenAsync()
        {
            try
            {
                string jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last(); ;


                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    _logger.LogInformation("Missing token");
                    return string.Empty;
                }

                return jwtToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(UserManagementService).Name, nameof(GetTokenAsync));
                return string.Empty;
            }
        }

        public Task<CustomResult<ExportStudentExamsDto>> ExportUsers(UserExportModel search)
        {
            try
            {
                var endDate = Data.Common.Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);

                var records = from u in _context.Users
                            join ur in _context.UserRoles on u.Id equals ur.UserId into urj
                            from ur in urj.DefaultIfEmpty()
                            join r in _context.Roles on ur.RoleId equals r.Id into rj
                            from r in rj.DefaultIfEmpty()
                            //join i in _context.Institutions on u.InstitutionId equals i.Id into ij
                            //from i in ij.DefaultIfEmpty()
                            where !u.IsDeleted
                            select new
                            {
                                User = u,
                                RoleName = r != null ? r.Name : null,
                                //InstitutionName = i != null ? i.Name : "n/a"
                            };
                // Exclude CANDIDATE users completely
                records = records.Where(x =>
                            !_context.UserRoles
                                .Join(_context.Roles,
                                      ur => ur.RoleId,
                                      r => r.Id,
                                      (ur, r) => new { ur.UserId, r.Name })
                                .Any(r => r.UserId == x.User.Id && r.Name == "CANDIDATE")
                        );

                //if (search.InstitutionId > 0)
                //{
                //    records = records.Where(x => x.User.InstitutionId == search.InstitutionId);
                //}
                if (!string.IsNullOrEmpty(search.RoleName))
                {
                    records = records.Where(x => x.RoleName == search.RoleName);
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.User.CreatedAt >= search.StartDate && x.User.CreatedAt <= endDate);
                }

                var result = records
                        .AsEnumerable()
                        .GroupBy(x => x.User.Id)
                        .Select(g => new UserExportListModel
                        {
                            PhoneNumber = string.IsNullOrEmpty(g.First().User.PhoneNumber)
                                ? "n/a" : g.First().User.PhoneNumber,
                            Email = g.First().User.Email,
                            UserName = g.First().User.UserName,
                            FullName = g.First().User.LastName + " " + g.First().User.FirstName,
                            //Institution = g.First().InstitutionName,
                            Role = g.Any(x => x.RoleName != null)
                                ? string.Join(",", g.Where(x => x.RoleName != null).Select(x => x.RoleName))
                                : "n/a",
                            Timestamp = g.First().User.CreatedAt.ToString("dd MMM yyyy HH:mm tt")
                        });

                if (!result.Any())
                {
                    return Task.FromResult(CustomResult<ExportStudentExamsDto>.ErrorOccured(
                            "No record found",
                            ResponseCodes.NotFoundErrorCode));
                }
                var sb = new StringBuilder();
                sb.AppendLine("FullName,Email,UserName,PhoneNumber,Role,Institution,DateCreated");

                foreach (var item in result)
                {
                    sb.AppendLine($"{item.FullName},{item.Email},{item.UserName},{item.PhoneNumber},{item.Role},{item.Institution},{item.Timestamp}");
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

                return Task.FromResult(CustomResult<ExportStudentExamsDto>.Success(exportDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult(CustomResult<ExportStudentExamsDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode));
            }
        }
    }
}
