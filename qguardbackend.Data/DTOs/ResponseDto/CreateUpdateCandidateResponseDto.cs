using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.DTOs.ResponseDto
{
 

    public class CreateUpdateCandidateResponseDto
    {

        public long CandidateId { get; set; }
        public string UserId { get; set; }
        public bool IsCreatedOrUpdated { get; set; }

    }
}
