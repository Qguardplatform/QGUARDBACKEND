using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.Entities
{
    public class City
    {
        public static City Create(String name, decimal latitutde, decimal longitude)
        {
            return new City()
            {
                Name = name,
                Latitude = latitutde,
                Longitude = longitude,
                IsActive = true,
                IsDeleted = false,
            };
        }

        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public Guid Region_Id { get; set; }
        [ForeignKey(nameof(Region_Id))]
        public Region Region { get; set; }
        public string CreatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
