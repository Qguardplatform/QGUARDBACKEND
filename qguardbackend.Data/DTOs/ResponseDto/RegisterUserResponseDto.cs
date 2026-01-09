using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.DTOs.ResponseDto
{
   
    public class RegisterUserResponseDto
    {
        public bool Status { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
    }
}
