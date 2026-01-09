using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class SemesterModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
    }
    public class SemesterCreateModel
    {
        [Required(ErrorMessage ="Semester name is required!")]
        [MinLength(3, ErrorMessage = "Semester name must be at least 3 characters long.")]
        [MaxLength(30, ErrorMessage = "Semester name cannot exceed 30 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "InstitutionId is required!")]
        public long InstitutionId { get; set; }
    }
}