using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.DTOs.ResponseDto
{
    public class InstitutionTutorsResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
