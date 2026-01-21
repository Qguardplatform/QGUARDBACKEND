using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Core.Interfaces
{
    public interface IReportlogService
    {
        Task<CustomResult<ReportLogResponseDto>> Create(ReportLogsRequestDto model, string createdBy);
        Task<CustomResult<ReportLogResponseDto>> Update(long id, ReportLogsRequestDto model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<PaginatedResult<ReportLogResponseDto>>> GetAll(QueryModelMini search);
        Task<CustomResult<ReportLogResponseDto>> GetById(long id);
    }
}
