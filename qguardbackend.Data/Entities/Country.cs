using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.Entities
{
    public class Country
    {
        public Country()
        {
            States = new Collection<Region>();
        }
        public static Country Create(String name, String capital, String currencySymbol, String phoneCode,
            String subRegion, String native, String region, String tid, String currency,
            decimal latitutde, decimal longitude, String emoji)
        {
            return new Country()
            {
                Name = name,
                Capital = capital,
                CurrencySymbol = currencySymbol,
                SubRegion = subRegion,
                PhoneCode = phoneCode,
                Native = native,
                Region = region,
                Tld = tid,
                Currency = currency,
                Latitude = latitutde,
                Longitude = longitude,
                Emoji = emoji,
                IsActive = true,
                IsDeleted = false,
            };
        }

        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Capital { get; set; }
        public string Currency { get; set; }
        public string CurrencySymbol { get; set; }
        public string SubRegion { get; set; }
        public string PhoneCode { get; set; }
        public string Native { get; set; }
        public string Region { get; set; }
        public string Tld { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Emoji { get; set; }
        public string CreatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public ICollection<Region> States { get; set; }
    }
}