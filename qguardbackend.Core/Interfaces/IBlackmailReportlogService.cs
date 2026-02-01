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
    public interface IBlackmailReportlogService
    {
        Task<CustomResult<BlackmailReportLogResponseDto>> Create(BlackmailReportLogsRequestDto model, string createdBy);
        Task<CustomResult<BlackmailReportLogResponseDto>> Update(long id, BlackmailReportLogsRequestDto model, string createdBy);
        Task<CustomResult<string>> Delete(long id, string createdBy);
        Task<CustomResult<PaginatedResult<BlackmailReportLogResponseDto>>> GetAll(QueryModelMini search);
        Task<CustomResult<BlackmailReportLogResponseDto>> GetById(long id);
    }
}
