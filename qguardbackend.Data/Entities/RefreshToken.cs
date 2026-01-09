using qguardbackend.Data.Abstracts;
using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.Entities
{
    public class RefreshToken : BaseEntity
    {
        // The ClientId, where it comes from
        [Required]
        public string ClientId { get; set; }
        // Value of the Token
        [Required]
        public string Value { get; set; }

        // The UserId it was issued to
        public Guid UserId { get; set; }

        // Get the Token Creation Date
        [Required]
        public DateTime CreatedDate { get; set; }
        [Required]
        public DateTime LastModifiedDate { get; set; }
        [Required]
        public DateTime ExpiryTime { get; set; }
    }
}
