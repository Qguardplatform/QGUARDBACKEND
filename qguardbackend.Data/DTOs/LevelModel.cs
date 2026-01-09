using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class LevelModel
    {
        public long Id { get; set; }
        public string LevelName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public InstitutionResponseDto Institution { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class LevelCreateModel
    {
        [Required(ErrorMessage ="Level name is required")]
        public string LevelName { get; set; }
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
        //public long InstitutionId { get; set; }
    }
}