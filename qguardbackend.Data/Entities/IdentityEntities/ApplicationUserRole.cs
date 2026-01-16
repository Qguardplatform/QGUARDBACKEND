using qguardbackend.Data.Entities;
using qguardbackend.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace qguardbackend.Data.Model;

public class ApplicationUserRole : IdentityUserRole<string>
{
    //public long? InstitutionId { get; set; }

    //[ForeignKey(nameof(InstitutionId))]
    //public Institution Institution { get; set; }
    public DateTime? CreatedAt { get; set; }
   
    public ApplicationRole Role { get; set; }
}