using qguardbackend.Data.Entities;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class CountryDto
    {
        [Required(ErrorMessage = "Country name is required")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Country capital is required")]
        public string Capital { get; set; }
        [Required(ErrorMessage = "Country currency is required")]
        public string Currency { get; set; }
        [Required(ErrorMessage = "Currency symbol is required")]
        public string CurrencySymbol { get; set; }
        [Required(ErrorMessage = "Phone code is required")]
        public string PhoneCode { get; set; }
        [Required(ErrorMessage = "Region is required")]
        public string Region { get; set; }
        public string SubRegion { get; set; }
        public string Native { get; set; }
        public string Tld { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Emoji { get; set; }
    }
    public class CountryListDto
    {
        public CountryListDto()
        {
            States = new Collection<RegionListDto>();
        }
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
        public DateTime DateCreated { get; set; }
        public ICollection<RegionListDto> States { get; set; }
    }
    public class RegionDto
    {
        [Required(ErrorMessage = "Region name is required")]
        public string Name { get; set; }
        [Required(ErrorMessage = "State code is required")]
        public string StateCode { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        [Required(ErrorMessage = "CountryId is required")]
        public Guid CountryId { get; set; }
    }
    public class RegionListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string StateCode { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public Guid CountryId { get; set; }
        public string CountryName { get; set; }
        public DateTime DateCreated { get; set; }
    }
    public class CityDto
    {

        [Required(ErrorMessage = "City name is required")]
        public string Name { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        [Required(ErrorMessage = "Region Id is required")]
        public Guid RegionId { get; set; }
    }
    public class CityListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public Guid RegionId { get; set; }
        public string RegionName { get; set; }
        public string Country { get; set; }
        public DateTime DateCreated { get; set; }
    }
    public class LGADto
    {

        [Required(ErrorMessage = "City name is required")]
        public string Name { get; set; }
        [Required(ErrorMessage = "City Id is required")]
        public Guid CityId { get; set; }
    }
    public class LGAListDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public Guid CityId { get; set; }
        public string logo_url { get; set; }
        public string CityName { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class CountryData
    {
        public CountryData()
        {
            States = new Collection<RegionData>();
        }

        public int Id { get; set; }
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
        public ICollection<RegionData> States { get; set; }

        public static explicit operator Country(CountryData source)
        {
            var destination = new Country()
            {
                Name = source.Name,
                Capital = source.Capital,
                Currency = source.Currency,
                CurrencySymbol = source.CurrencySymbol,
                SubRegion = source.SubRegion,
                PhoneCode = source.PhoneCode,
                Native = source.Native,
                Region = source.Region,
                Tld = source.Tld,
                Latitude = source.Latitude,
                Longitude = source.Longitude,
                Emoji = source.Emoji
            };

            return destination;
        }
    }

    public class RegionData
    {
        public RegionData()
        {
            Cities = new Collection<CityData>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string StateCode { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public CountryData Country { get; set; }
        public virtual ICollection<CityData> Cities { get; set; }

        public static explicit operator Region(RegionData source)
        {
            var destination = new Region()
            {
                Name = source.Name,
                StateCode = source.StateCode,
                Latitude = source.Latitude,
                Longitude = source.Longitude
            };
            return destination;
        }
    }

    public class CityData
    {
        public CityData()
        {
            Region = new Collection<RegionData>();
        }

        public long Id { get; set; }
        public string Name { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public CountryData Country { get; set; }
        public Collection<RegionData> Region { get; set; }

        public static explicit operator City(CityData source)
        {
            var destination = new City()
            {
                Id = Guid.NewGuid(),
                Name = source.Name,
                Latitude = source.Latitude,
                Longitude = source.Longitude
            };
            return destination;
        }
    }
}
