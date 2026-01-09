using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace qguardbackend.Core.Services
{
    public class StaticDatas : IStaticDatas
    {
        private readonly AppDbContext _context;

        public StaticDatas(AppDbContext context)
        {
            _context = context;
        }
        public async Task<CustomResult<CityListDto>> AddCity(CityDto city)
        {
            bool check = await this.CheckCityByName(city.Name, city.RegionId);
            if (check == true)
            {
                return CustomResult<CityListDto>.ErrorOccured("City already exist", ResponseCodes.AlreadyExistErrorCode);
            }
            var create = City.Create(city.Name, city.Latitude, city.Longitude);
            create.Id = Guid.NewGuid();
            create.Region_Id = city.RegionId;
            await _context.AddAsync(create);
            await _context.SaveChangesAsync();
            var returnDto = await this.GetCity(create.Id);
            return returnDto;
        }

        public async Task<CustomResult<CountryListDto>> AddCountry(CountryDto country)
        {
            bool check = await this.CheckCountryByName(country.Name);
            if (check == true)
            {
                return CustomResult<CountryListDto>.ErrorOccured("Country already exist", ResponseCodes.AlreadyExistErrorCode);
            }
            var create = Country.Create(country.Name, country.Capital, country.CurrencySymbol, country.PhoneCode, country.SubRegion, country.Native,
                country.Region, country.Tld, country.Currency, country.Latitude, country.Longitude, country.Emoji);
            create.Id = Guid.NewGuid();
            await _context.AddAsync(create);
            await _context.SaveChangesAsync();
            var returnDto = await this.GetCountry(create.Id);
            return returnDto;
        }

        public async Task<CustomResult<RegionListDto>> AddRegion(RegionDto region)
        {
            bool check = await this.CheckRegionByName(region.Name, region.CountryId);
            if (check == true)
            {
                return CustomResult<RegionListDto>.ErrorOccured("State already exist", ResponseCodes.AlreadyExistErrorCode);
            }
            var create = Region.Create(region.Name, region.StateCode, region.Latitude, region.Longitude);
            create.Id = Guid.NewGuid();
            create.Country_Id = region.CountryId;
            await _context.AddAsync(create);
            await _context.SaveChangesAsync();
            var returnDto = await this.GetRegion(create.Id);
            return returnDto;
        }

        public async Task<bool> CheckCityById(Guid id, Guid regionId)
        {
            var city = await _context.Cities.FirstOrDefaultAsync(c => c.Id == id && c.Region_Id == regionId);
            if (city == null) return false;
            return true;
        }

        public async Task<bool> CheckCityByName(string name, Guid regionId)
        {
            var city = await _context.Cities.FirstOrDefaultAsync(c => c.Name == name && c.Region_Id == regionId);
            if (city == null) return false;
            return true;
        }

        public async Task<bool> CheckCountryById(Guid id)
        {
            var country = await _context.Countries.FirstOrDefaultAsync(c => c.Id == id);
            if (country == null) return false;
            return true;
        }

        public async Task<bool> CheckCountryByName(string name)
        {
            var country = await _context.Countries.FirstOrDefaultAsync(c => c.Name == name);
            if (country == null) return false;
            return true;
        }

        public async Task<bool> CheckRegionById(Guid id, Guid countryId)
        {
            var region = await _context.Regions.FirstOrDefaultAsync(c => c.Id == id && c.Country_Id == countryId);
            if (region == null) return false;
            return true;
        }

        public async Task<bool> CheckRegionByName(string name, Guid countryId)
        {
            var region = await _context.Regions.FirstOrDefaultAsync(c => c.Name == name && c.Country_Id == countryId);
            if (region == null) return false;
            return true;
        }

        public async Task<CustomResult<string>> DeleteCity(Guid id)
        {
            var city = await _context.Cities.FirstOrDefaultAsync(x => x.Id == id);
            if (city == null)
            {
                return CustomResult<string>.ErrorOccured("City not found!", ResponseCodes.NotFoundErrorCode);
            }

            _context.Remove(city);
            await _context.SaveChangesAsync();
            return CustomResult<string>.Success("City successfully deleted", ResponseCodes.SuccessCode);
        }

        public async Task<CustomResult<string>> DeleteCountry(Guid id)
        {
            var country = await _context.Countries.FirstOrDefaultAsync(x => x.Id == id);
            if (country == null)
            {
                return CustomResult<string>.ErrorOccured("Country not found!", ResponseCodes.NotFoundErrorCode);
            }
            var check = await _context.Regions.Where(x => x.Country_Id == id).ToListAsync();
            if (check.Any())
            {
                return CustomResult<string>.ErrorOccured("Country cannot be deleted because it has already been mapped with a state!", ResponseCodes.BadRequestErrorCode);
            }
            _context.Remove(country);
            await _context.SaveChangesAsync();
            return CustomResult<string>.Success("Country successfully deleted", ResponseCodes.SuccessCode);
        }

        public async Task<CustomResult<string>> DeleteRegion(Guid id)
        {
            var region = await _context.Regions.FirstOrDefaultAsync(x => x.Id == id);
            if (region == null)
            {
                return CustomResult<string>.ErrorOccured("State not found!", ResponseCodes.NotFoundErrorCode);
            }
            var check = await _context.Cities.Where(x => x.Region_Id == id).ToListAsync();
            if (check.Any())
            {
                return CustomResult<string>.ErrorOccured("State cannot be deleted because it has already been mapped with a city!", ResponseCodes.BadRequestErrorCode);
            }
            _context.Remove(region);
            await _context.SaveChangesAsync();
            return CustomResult<string>.Success("Region successfully deleted", ResponseCodes.SuccessCode);
        }

        public async Task<CustomResult<PaginatedResult<CityListDto>>> GetAllCities(QueryModelMini search)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<City> query = _context.Cities.Include(x => x.Region).ThenInclude(x => x.Country);
                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    query = query.Where(x => x.Name.ToLower().Contains(searchWord));
                }

                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    query = query.Where(x => x.CreatedOn >= search.StartDate && x.CreatedOn <= endDate);
                }

                if (search.Sorting?.ToLower() == "asc")
                {
                    query = query.OrderBy(x => x.Name);
                }
                else
                {
                    query = query.OrderByDescending(x => x.CreatedOn);
                }

                var result = await query.Select(countries => new CityListDto
                {
                    Id = countries.Id,
                    Name = countries.Name == null ? "n/a" : countries.Name,
                    Longitude = countries.Longitude,
                    Latitude = countries.Longitude,
                    RegionId = countries.Region_Id,
                    RegionName = countries.Region == null ? "n/a" : countries.Region.Name,
                    Country = countries.Region.Country == null ? "n/a" : countries.Region.Country.Name,
                    DateCreated = countries.CreatedOn
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<CityListDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                return CustomResult<PaginatedResult<CityListDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<CityListDto>>> GetAllCitiesByRegionId(Guid regionId, QueryModelMini search)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<City> query = _context.Cities.Include(x => x.Region).Where(x => x.Region_Id == regionId);

                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    query = query.Where(x => x.Name.ToLower().Contains(searchWord) || x.Region.Name.ToLower().Contains(searchWord));
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    query = query.Where(x => x.CreatedOn >= search.StartDate && x.CreatedOn <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    query = query.OrderBy(x => x.Name);
                }
                else
                {
                    query = query.OrderByDescending(x => x.CreatedOn);
                }
                var result = await query.Select(countries => new CityListDto
                {
                    Id = countries.Id,
                    Name = countries.Name == null ? String.Empty : countries.Name ?? String.Empty,
                    Longitude = countries.Longitude,
                    Latitude = countries.Latitude,
                    RegionId = countries.Region_Id,
                    RegionName = countries.Region == null ? String.Empty : countries.Region.Name ?? String.Empty,
                    DateCreated = countries.CreatedOn
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<CityListDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                return CustomResult<PaginatedResult<CityListDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<CountryListDto>>> GetAllCountries(QueryModelMini search)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Country> query = _context.Countries.Include(x => x.States);

                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    query = query.Where(x =>
                        x.Name.ToLower().Contains(searchWord) ||
                        x.Capital.ToLower().Contains(searchWord) ||
                        x.Native.ToLower().Contains(searchWord) ||
                        x.Currency.ToLower().Contains(searchWord));
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    query = query.Where(x => x.CreatedOn >= search.StartDate && x.CreatedOn <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    query = query.OrderBy(x => x.Name);
                }
                else
                {
                    query = query.OrderByDescending(x => x.CreatedOn);
                }

                var result = await query.Select(countries => new CountryListDto
                {
                    Id = countries.Id,
                    Name = countries.Name == null ? String.Empty : countries.Name ?? String.Empty,
                    Native = countries.Native == null ? String.Empty : countries.Native ?? String.Empty,
                    Capital = countries.Capital == null ? String.Empty : countries.Capital ?? String.Empty,
                    CurrencySymbol = countries.CurrencySymbol == null ? String.Empty : countries.CurrencySymbol ?? String.Empty,
                    Currency = countries.Currency == null ? String.Empty : countries.Currency ?? String.Empty,
                    Region = countries.Region == null ? String.Empty : countries.Region ?? String.Empty,
                    SubRegion = countries.SubRegion == null ? String.Empty : countries.SubRegion ?? String.Empty,
                    Longitude = countries.Longitude,
                    Latitude = countries.Latitude,
                    PhoneCode = countries.PhoneCode == null ? String.Empty : countries.PhoneCode ?? String.Empty,
                    Tld = countries.Tld == null ? String.Empty : countries.Tld ?? String.Empty,
                    Emoji = countries.Emoji == null ? String.Empty : countries.Emoji ?? String.Empty,
                    DateCreated = countries.CreatedOn
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<CountryListDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                return CustomResult<PaginatedResult<CountryListDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<RegionListDto>>> GetAllRegions(QueryModelMini search)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Region> query = _context.Regions.Include(x => x.Country);

                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    query = query.Where(x =>
                        x.Name.ToLower().Contains(searchWord) ||
                        x.StateCode.ToLower().Contains(searchWord) ||
                        x.Country.Name.ToLower().Contains(searchWord));
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    query = query.Where(x => x.CreatedOn >= search.StartDate && x.CreatedOn <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    query = query.OrderBy(x => x.Name);
                }
                else
                {
                    query = query.OrderByDescending(x => x.CreatedOn);
                }
                var result = await query.Select(countries => new RegionListDto
                {
                    Id = countries.Id,
                    Name = countries.Name == null ? String.Empty : countries.Name ?? String.Empty,
                    StateCode = countries.StateCode == null ? String.Empty : countries.StateCode ?? String.Empty,
                    Longitude = countries.Longitude,
                    Latitude = countries.Latitude,
                    CountryId = countries.Country_Id,
                    CountryName = countries.Country == null ? String.Empty : countries.Country.Name ?? String.Empty,
                    DateCreated = countries.CreatedOn
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<RegionListDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                return CustomResult<PaginatedResult<RegionListDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<RegionListDto>>> GetAllRegionsByCountryId(Guid countryId, QueryModelMini search)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Region> query = _context.Regions.Where(x => x.Country_Id == countryId);

                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    query = query.Where(x =>
                        x.Name.ToLower().Contains(searchWord) ||
                        x.StateCode.ToLower().Contains(searchWord) ||
                        x.Country.Name.ToLower().Contains(searchWord));
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    query = query.Where(x => x.CreatedOn >= search.StartDate && x.CreatedOn <= endDate);

                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    query = query.OrderBy(x => x.Name);
                }
                else
                {
                    query = query.OrderByDescending(x => x.CreatedOn);
                }

                var result = await query.Select(countries => new RegionListDto
                {
                    Id = countries.Id,
                    Name = countries.Name == null ? String.Empty : countries.Name ?? String.Empty,
                    StateCode = countries.StateCode == null ? String.Empty : countries.StateCode ?? String.Empty,
                    Longitude = countries.Longitude,
                    Latitude = countries.Latitude,
                    CountryId = countries.Country_Id,
                    DateCreated = countries.CreatedOn
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<RegionListDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                return CustomResult<PaginatedResult<RegionListDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<CityListDto>> GetCity(Guid id)
        {
            var city = await _context.Cities.Include(x => x.Region).FirstOrDefaultAsync(c => c.Id == id);
            if (city == null)
            {
                return CustomResult<CityListDto>.ErrorOccured("Invalid City Id!", ResponseCodes.BadRequestErrorCode);
            }
            var model = new CityListDto
            {
                Id = city.Id,
                Name = city.Name,
                Longitude = city.Longitude,
                Latitude = city.Latitude,
                RegionId = city.Region_Id,
                RegionName = city.Region == null ? string.Empty : city.Region.Name ?? string.Empty,
                DateCreated = city.CreatedOn
            };
            return CustomResult<CityListDto>.Success(model);
        }

        public async Task<CustomResult<CountryListDto>> GetCountry(Guid id)
        {
            var model = await _context.Countries.FirstOrDefaultAsync(c => c.Id == id);
            if (model == null)
            {
                return CustomResult<CountryListDto>.ErrorOccured("Invalid Country Id!", ResponseCodes.BadRequestErrorCode);
            }
            var country = new CountryListDto
            {
                Id = model.Id,
                Name = model.Name,
                Native = model.Native,
                Capital = model.Capital,
                Currency = model.Currency,
                CurrencySymbol = model.CurrencySymbol,
                Region = model.Region,
                SubRegion = model.SubRegion,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                PhoneCode = model.PhoneCode,
                Tld = model.Tld,
                Emoji = model.Emoji,
                DateCreated = model.CreatedOn
            };
            return CustomResult<CountryListDto>.Success(country);
        }

        public async Task<CustomResult<RegionListDto>> GetRegion(Guid id)
        {
            var model = await _context.Regions.Include(x => x.Country).FirstOrDefaultAsync(c => c.Id == id);
            if (model == null)
            {
                return CustomResult<RegionListDto>.ErrorOccured("Invalid Region Id!", ResponseCodes.BadRequestErrorCode);
            }
            var region = new RegionListDto
            {
                Id = model.Id,
                Name = model.Name,
                StateCode = model.StateCode,
                Longitude = model.Longitude,
                Latitude = model.Latitude,
                CountryId = model.Country_Id,
                DateCreated = model.CreatedOn
            };
            return CustomResult<RegionListDto>.Success(region);
        }

        public async Task<CustomResult<RegionListDto>> GetRegionByName(string name)
        {
            var model = await _context.Regions.Include(x => x.Country).FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
            if (model == null)
            {
                return CustomResult<RegionListDto>.ErrorOccured("Invalid Region name!", ResponseCodes.BadRequestErrorCode);
            }
            var region = new RegionListDto
            {
                Id = model.Id,
                Name = model.Name,
                StateCode = model.StateCode,
                Longitude = model.Longitude,
                Latitude = model.Latitude,
                CountryId = model.Country_Id,
                DateCreated = model.CreatedOn
            };
            return CustomResult<RegionListDto>.Success(region);
        }

        public async Task SeedDefaultCountry()
        {
            var fullPath = System.IO.File.ReadAllText("Filter/SeedUtility/CountryData.json");

            List<CountryData>   countryViewModel = JsonConvert.DeserializeObject<List<CountryData>>(fullPath);

            if (_context.Countries.Any())
            {
                return;
            }
            foreach (var vm in countryViewModel)
            {
                var country = (Country)vm;
                country.Id = Guid.NewGuid();
                country.CreatedBy = "seed";
                await _context.AddAsync(country);

                foreach (var r in vm.States)
                {
                    var region = (Region)r;
                    region.Id = Guid.NewGuid();
                    region.Country_Id = country.Id;
                    region.CreatedBy = country.CreatedBy;
                    await _context.AddAsync(region);

                    foreach (var cit in r.Cities)
                    {
                        var city = (City)cit;
                        city.Id = Guid.NewGuid();
                        city.Region_Id = region.Id;
                        city.CreatedBy = country.CreatedBy;
                        await _context.AddAsync(city);
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<CustomResult<CityListDto>> UpdateCity(Guid id, CityDto city)
        {
            var query = await _context.Cities.FirstOrDefaultAsync(x => x.Id == id);
            if (query == null)
            {
                return CustomResult<CityListDto>.ErrorOccured("City Id does not exist!", ResponseCodes.BadRequestErrorCode);
            }
            query.Name = city.Name;
            query.Longitude = city.Longitude;
            query.Latitude = city.Latitude;

            _context.Update(query);
            await _context.SaveChangesAsync();
            var returnDto = await this.GetCity(id);
            return returnDto;
        }

        public async Task<CustomResult<CountryListDto>> UpdateCountry(Guid id, CountryDto country)
        {
            var query = await _context.Countries.FirstOrDefaultAsync(x => x.Id == id);
            if (query == null)
            {
                return CustomResult<CountryListDto>.ErrorOccured("Country Id does not exist!", ResponseCodes.BadRequestErrorCode);
            }
            query.Name = country.Name;
            query.Native = country.Native;
            query.Capital = country.Capital;
            query.Currency = country.Currency;
            query.CurrencySymbol = country.CurrencySymbol;
            query.Emoji = country.Emoji;
            query.SubRegion = country.SubRegion;
            query.Region = country.Region;
            query.Tld = country.Tld;
            query.PhoneCode = country.PhoneCode;
            query.Longitude = country.Longitude;
            query.Latitude = country.Latitude;

            _context.Update(query);
            await _context.SaveChangesAsync();
            var returnDto = await this.GetCountry(id);
            return returnDto;
        }

        public async Task<CustomResult<RegionListDto>> UpdateRegion(Guid id, RegionDto region)
        {
            var query = await _context.Regions.FirstOrDefaultAsync(x => x.Id == id);
            if (query == null)
            {
                return CustomResult<RegionListDto>.ErrorOccured("Region Id does not exist!", ResponseCodes.BadRequestErrorCode);
            }
            query.Name = region.Name;
            query.StateCode = region.StateCode;
            query.Longitude = region.Longitude;
            query.Latitude = region.Latitude;

            _context.Update(query);
            await _context.SaveChangesAsync();
            var returnDto = await this.GetRegion(id);
            return returnDto;
        }
    }
}
