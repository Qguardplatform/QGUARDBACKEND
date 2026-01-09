using qguardbackend.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class ProgramModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProgramType { get; set; }
        public long noOfSemesters { get; set; }
        public long DurationInMonths { get; set; }

        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
    }
    public class ProgramCreateModel
    {
        [Required(ErrorMessage = "Program name is required!")]
        public string Name { get; set; }
        public long noOfSemesters { get; set; }
        public ProgramTypeEnum? ProgramType { get; set; }
        //public string? ProgramType { get; set; }
        public long DurationInMonths { get; set; }
        //[Required(ErrorMessage = "InstitutionId is required!")]
        //public long InstitutionId { get; set; }
    }
}
