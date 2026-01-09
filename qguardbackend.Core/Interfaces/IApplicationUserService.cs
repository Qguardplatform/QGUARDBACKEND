using LS1.Data.Models;
using LS1.Shared.Pagination;
using UtilityLibrary.Models;
using LS1.Core.Helpers.Autofac;
using LS1_Backend.LS1.Shared.DTOs.Results;
using LS1_Backend.LS1.Shared.DTOs.RequestDtos;
using LS1.Data.Model;
using LS1_Backend.LS1.Shared.DTOs.ResponseDtos;

namespace LS1.Core.Interfaces;

public interface IApplicationUserService : IAutoDependencyCore
{
    Task<CustomResult<ApplicationUserResponse>> DeleteAsync(Guid id);
    Task<CustomResult<List<ApplicationUserResponse>>> GetUsersAsync();
    Task<CustomResult<List<ApplicationRoleResponse>>> GetRolesAsync();
    Task<CustomResult<ApplicationUserResponse>> GetItemAsync(Guid id);
    Task<CustomResult<ApplicationUserResponse>> EnableAsync(Guid id);
    Task<CustomResult<ApplicationUserResponse>> DisableAsync(Guid id);
    Task<CustomResult<PagedList<ApplicationUserResponse>>> GetUsersListByRoleAsync(DateRangeQueryModel query, Guid roleId);
    Task<CustomResult<PagedList<ApplicationUserResponse>>> ApprovedListAsync(DateRangeQueryModel query);
    Task<CustomResult<PagedList<ApplicationUserResponse>>> PendingListAsync(DateRangeQueryModel query);
    Task<CustomResult<PagedList<ApplicationUserResponse>>> RejectedListAsync(DateRangeQueryModel query);
    Task<CustomResult<ApplicationUserSignUpResponse>> InviteAsync(ApplicationUserInviteRequest applicationUserInviteRequest, DateTime currentDateTime, string currentUser);
    //Task<CustomResult<ApplicationUserResponse>> InviteAsync(ApplicationUserInviteRequest applicationUserInviteRequest, DateTime currentDateTime, string currentUser);
    Task<CustomResult<ApplicationUserResponse>> UpdateAsync(Guid id, ApplicationUserUpdateRequest applicationUserUpdateRequest, DateTime currentDateTime);
    Task<CustomResult<ApplicationUserResponse>> ApproveAsync(Guid id, ApprovalRequest approvalRequest, string currentUserId, DateTime currentDateTime);
    Task<CustomResult<ApplicationUserResponse>> RejectAsync(Guid id, ApprovalRequest approvalRequest, string currentUserId, DateTime currentDateTime);
    Task<CustomResult<PagedList<ApplicationUserResponse>>> SearchAsync(DateRangeQueryModel query);
    Task<CustomResult<List<ApplicationUserResponse>>> ExportDataAsync(ExportQueryModel query);
    Task<CustomResult<List<ApplicationUserResponse>>> ExportDataApprovedListAsync(ExportQueryModel query);
    Task<CustomResult<List<ApplicationUserResponse>>> ExportDataRejectedListAsync(ExportQueryModel query);
    Task<CustomResult<List<ApplicationUserResponse>>> ExportDataPendingListAsync(ExportQueryModel query);
    Task<CustomResult<CountAnalyticResponse>> GetTotalNumberOfUsersAsync(DateRangeQueryModel queryModel);
    Task<CustomResult<CountAnalyticResponse>> GetTotalNumberOfUsersPendingApprovalAsync(DateRangeQueryModel queryModel);
    Task<CustomResult<CountAnalyticResponse>> GetTotalNumberOfUsersApprovedAsync(DateRangeQueryModel queryModel);
    Task<CustomResult<CountAnalyticResponse>> GetTotalNumberOfUsersRejectedAsync(DateRangeQueryModel queryModel);
    Task<CustomResult<CountAnalyticResponse>> GetTotalNumberOfRolesAsync();
    Task<CustomResult<ApplicationUserResponseDto>> GetLoggedInUser();
    Task<CustomResult<string>> GetLoggedInUserEmail();
    Task<CustomResult<ApplicationUserSignUpResponse>> InviteAsync(UserInvitationRequestDto model);
}
