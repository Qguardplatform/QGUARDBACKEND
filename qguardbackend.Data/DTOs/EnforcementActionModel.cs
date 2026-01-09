using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class EnforcementActionModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class EnforcementActionCreateModel
    {
        [Required(ErrorMessage = "Enforcement action name is required!")]
        public string Name { get; set; }
    }
    public class EnforcementModeModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class EnforcementModeCreateModel
    {
        [Required(ErrorMessage = "Enforcement mode name is required!")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Enforcement mode code is required!")]
        public string Code { get; set; }
        public bool IsActive { get; set; }
    }
}
