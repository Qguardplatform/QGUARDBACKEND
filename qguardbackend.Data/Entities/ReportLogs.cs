
using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class ReportLogs : BaseEntity
    {
        public static ReportLogs Create(

            String Name, String Socials, String Description,
            String Address, String NearestBustop, String City,
            String LGA, String State, String Country

            )
        {
            return new ReportLogs()
            {
                Name = Name,
                Socials = Socials,
                Description = Description,
                City = City,
                LGA = LGA,
                State = State,
                Country = Country,
                NearestBustop = NearestBustop,
                Address = Address,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }

        public string Name { get; set; }
        public string Socials { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string NearestBustop { get; set; }
        public string City { get; set; }
        public string LGA { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }
}
