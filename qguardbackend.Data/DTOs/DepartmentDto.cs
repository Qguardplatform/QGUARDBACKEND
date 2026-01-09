using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class DepartmentDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public DateTime DateCreated { get; set; }
        public FacultyDto FacultyDetails { get; set; }

    }
    public class DepartmentCreateModel
    {
        [Required(ErrorMessage = "Department name is required!")]
        public string Name { get; set; }

        [Required(ErrorMessage = "facultyId is required!")]
        public long FacultyId { get; set; }
    }
}
