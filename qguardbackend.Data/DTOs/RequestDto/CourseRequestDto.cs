using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.DTOs.RequestDto
{
    public class CourseRequestDto
    {
        public string Name { get; set; }
        public string CourseCode { get; set; }
        public string Description { get; set; }
        //public long? InstitutionId { get; set; }
        public List<long>? TutorsIds { get; set; }
        public List<long>? LevelsIds { get; set; }
        public List<long>? DepartmentIds { get; set; }




    }



    public class CourseResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string CourseCode { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public long? InstitutionId { get; set; }
        public string InstitutionName { get; set; }
        public List<long> TutorsIds { get; set; }
        public List<long> LevelsIds { get; set; }
        public List<long> DepartmentIds { get; set; }



    }


}
