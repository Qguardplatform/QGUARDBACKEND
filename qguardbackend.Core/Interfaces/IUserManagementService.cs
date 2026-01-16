using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Shared.Pagination;
using qguardbackend.Data.Entities;
using Microsoft.AspNetCore.Http;
using qguardbackend.Data.DTOs.RequestDto;

namespace qguardbackend.Core.Interfaces
{
    public interface IUserManagementService
    {
        Task<CustomResult<string>> ChangeAccountStatus(string userId);
        Task<bool> CheckIfAccountIsActive(string id);
        Task<string> ValidateRefreshToken(string clientId, string refreshToken);
        Task TokenCreateModel(Guid userId, RefreshToken token);
        Task UpdateUserLastLoginDate(string userId);
        Task SeedDefaultUser();
        Task<CustomResult<PagedList<ApplicationUserResponse>>> GetActiveUsersListAsync(QueryModelMini query);
        Task<CustomResult<PagedList<ApplicationUserResponse>>> GetUsersWithRolesAsync(UsersFilterModel query);
        Task<CustomResult<ApplicationUserResponse>> GetUserDetailsByIdAsync(string userId);
        Task<CustomResult<ApplicationUserResponse>> GetLoggedInUser();
        Task<CustomResult<long>> GetTenantId();
        Task<string> GetTokenAsync();
        Task<AuthClaims> GetUserClaim();
        Task<CustomResult<string>> GetTenantCode();
        void SetUserClaims(HttpContext context, string userId, string role);
        Task<string> generateResetTokenAsync();
        Task<string> generateVerificationTokenAsync();
        Task<string> GenerateRefreshToken(string email, string SAPId);
        Task<LoginResponse> GenerateJwtToken(string email, string applicationCode, ICollection<ApplicationRole> userRoles);
        Task<CustomResult<ExportStudentExamsDto>> ExportUsers(UserExportModel search);
    }
}