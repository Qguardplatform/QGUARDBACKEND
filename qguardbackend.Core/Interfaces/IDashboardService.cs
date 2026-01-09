using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;

namespace qguardbackend.Core.Interfaces
{
    public interface IDashboardService
    {
        Task<CustomResult<DashboardResponse>> GetInstitutionAdminDashboardAsync(DashboardFilterModel filter);
        Task<CustomResult<ExamPerformanceDto>> GetInstitutionExamPerformanceAsync(DashboardFilterModel filter);
        Task<CustomResult<AdminDashboardResponse>> GetAdminDashboardAsync(DashboardFilterModel filter);
    }
}