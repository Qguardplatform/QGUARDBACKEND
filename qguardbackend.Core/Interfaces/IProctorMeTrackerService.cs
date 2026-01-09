using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;

namespace qguardbackend.Core.Interfaces
{
    public interface IProctorMeTrackerService
    {
        Task<CustomResult<ProctorMeTrackerModel>> Create(ProctorMeTrackerModel model);
        Task<CustomResult<PaginatedResult<ProctorMeTrackerModel>>> GetAll(QueryModelMini search); 
    }
}