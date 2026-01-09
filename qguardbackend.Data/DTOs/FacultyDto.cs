using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class FacultyDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
        public List<ProgramModel> FacultyPrograms { get; set; }
        
    }
    public class FalcultyCreateModel
    {
        [Required(ErrorMessage = "Faculty name is required!")]
        [MinLength(3, ErrorMessage = "Faculty name must be at least 3 characters long.")]
        [MaxLength(100, ErrorMessage = "Faculty name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "InstitutionId is required!")]
        public long InstitutionId { get; set; }
        public List<FacultyCreateProgramModel> ProgramIds { get; set; }
    }

    public class FacultyCreateProgramModel
    {
        public long ProgramId { get; set; }
    }
}