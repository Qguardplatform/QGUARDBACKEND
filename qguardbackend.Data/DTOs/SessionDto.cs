using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class SessionDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
    }
    public class SessionCreateModel
    {
        [Required(ErrorMessage = "Session name is required!")]
        public string Name { get; set; }

        [Required(ErrorMessage = "InstitutionId is required!")]
        public long InstitutionId { get; set; }
    }
}
