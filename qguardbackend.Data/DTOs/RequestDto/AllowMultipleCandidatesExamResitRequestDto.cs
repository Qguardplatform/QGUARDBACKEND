using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.DTOs.RequestDto
{
    public class AllowMultipleCandidatesExamResitRequestDto
    {
        public long ExamScheduleId { get; set; }
        public List<long> CandidatesIds { get; set; }
    }
}
