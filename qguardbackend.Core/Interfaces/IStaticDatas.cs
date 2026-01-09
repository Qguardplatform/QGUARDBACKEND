using qguardbackend.Core.Autofac;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;

namespace qguardbackend.Core.Interfaces
{
    public interface IStaticDatas //: IAutoDependencyCore
    {
        #region Country
        Task<CustomResult<CountryListDto>> AddCountry(CountryDto country);
        Task<CustomResult<PaginatedResult<CountryListDto>>> GetAllCountries(QueryModelMini search);
        Task<CustomResult<CountryListDto>> GetCountry(Guid id);
        Task<CustomResult<CountryListDto>> UpdateCountry(Guid id, CountryDto country);
        Task<CustomResult<string>> DeleteCountry(Guid id);
        Task<bool> CheckCountryById(Guid id);
        Task<bool> CheckCountryByName(string name);
        #endregion

        #region Region
        Task<CustomResult<RegionListDto>> AddRegion(RegionDto region);
        Task<CustomResult<PaginatedResult<RegionListDto>>> GetAllRegionsByCountryId(Guid countryId, QueryModelMini search);
        Task<CustomResult<PaginatedResult<RegionListDto>>> GetAllRegions(QueryModelMini search);
        Task<CustomResult<RegionListDto>> GetRegion(Guid id);
        Task<CustomResult<RegionListDto>> GetRegionByName(string name);
        Task<CustomResult<RegionListDto>> UpdateRegion(Guid id, RegionDto region);
        Task<CustomResult<string>> DeleteRegion(Guid id);
        Task<bool> CheckRegionById(Guid id, Guid countryId);
        Task<bool> CheckRegionByName(string name, Guid countryId);
        #endregion

        #region City
        Task<CustomResult<CityListDto>> AddCity(CityDto city);
        Task<CustomResult<PaginatedResult<CityListDto>>> GetAllCitiesByRegionId(Guid regionId, QueryModelMini search);
        Task<CustomResult<PaginatedResult<CityListDto>>> GetAllCities(QueryModelMini search);
        Task<CustomResult<CityListDto>> GetCity(Guid id);
        Task<CustomResult<CityListDto>> UpdateCity(Guid id, CityDto city);
        Task<CustomResult<string>> DeleteCity(Guid id);
        Task<bool> CheckCityById(Guid id, Guid regionId);
        Task<bool> CheckCityByName(string name, Guid regionId);
        #endregion

        Task SeedDefaultCountry();
    }
}