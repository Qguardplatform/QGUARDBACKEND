using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace edutech.services.examportal.Data.Entities
{
    public class State
    {
        public State()
        {
            Cities = new Collection<City>();
        }

        public static State Create(String name, String stateCode, decimal latitutde, decimal longitude)
        {
            return new State()
            {
                Name = name,
                StateCode = stateCode,
                Latitude = latitutde,
                Longitude = longitude,
                IsActive = true,
                IsDeleted = false,
            };
        }

        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string StateCode { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string CreatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid Country_Id { get; set; }
        [ForeignKey(nameof(Country_Id))]
        public Country Country { get; set; }
        public virtual ICollection<City> Cities { get; set; }
    }
}
