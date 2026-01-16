using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace qguardbackend.Controllers.v1
{
    [ApiVersion("1.0")]
    public class StaticDataController : BaseController
    {
        private readonly IStaticDatas _staticRepo;

        public StaticDataController(IStaticDatas staticRepo)
        {
            _staticRepo = staticRepo;
        }

        [AllowAnonymous]
        [HttpGet("countries")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<CountryListDto>>>))]
        public async Task<IActionResult> GetCountries([FromQuery] QueryModelMini search)
        {
            var response = await _staticRepo.GetAllCountries(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("country/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CountryListDto>))]
        public async Task<IActionResult> GetCountryById([FromRoute] Guid id)
        {
            var response = await _staticRepo.GetCountry(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("states")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<RegionListDto>>>))]
        public async Task<IActionResult> GetRegions([FromQuery] QueryModelMini search)
        {
            var response = await _staticRepo.GetAllRegions(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("state/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<RegionListDto>))]
        public async Task<IActionResult> GetRegionById([FromRoute] Guid id)
        {
            var response = await _staticRepo.GetRegion(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("state/by/country/{countryid:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<RegionListDto>>>))]
        public async Task<IActionResult> GetRegionsByCountryId(Guid countryid, [FromQuery] QueryModelMini search)
        {
            var response = await _staticRepo.GetAllRegionsByCountryId(countryid, search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("cities")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<CityListDto>>>))]
        public async Task<IActionResult> GetCities([FromQuery] QueryModelMini search)
        {
            var response = await _staticRepo.GetAllCities(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("city/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CityListDto>))]
        public async Task<IActionResult> GetCityById([FromRoute] Guid id)
        {
            var response = await _staticRepo.GetCity(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("city/by/state/{regionid:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<CityListDto>>>))]
        public async Task<IActionResult> GetCitiesByRegionId([FromRoute] Guid regionid, [FromQuery] QueryModelMini search)
        {
            var response = await _staticRepo.GetAllCitiesByRegionId(regionid, search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("country/create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CountryListDto>))]
        public async Task<IActionResult> CreateCountry([FromBody] CountryDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await _staticRepo.AddCountry(model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("state/create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<RegionListDto>))]
        public async Task<IActionResult> CreateRegion([FromBody] RegionDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await _staticRepo.AddRegion(model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("city/create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CityListDto>))]
        public async Task<IActionResult> CreateCity([FromBody] CityDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await _staticRepo.AddCity(model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("country/update/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CountryListDto>))]
        public async Task<IActionResult> UpdateCountry(Guid id, [FromBody] CountryDto model)
        {
            var response = await _staticRepo.UpdateCountry(id, model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("state/update/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<RegionListDto>))]
        public async Task<IActionResult> EditRegion(Guid id, [FromBody] RegionDto model)
        {
            var response = await _staticRepo.UpdateRegion(id, model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("city/update/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CityListDto>))]
        public async Task<IActionResult> EditCity(Guid id, [FromBody] CityDto model)
        {
            var response = await _staticRepo.UpdateCity(id, model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("country/{id:guid}")]
        public async Task<IActionResult> DeleteCountry(Guid id)
        {
            var response = await _staticRepo.DeleteCountry(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("state/{id:guid}")]
        public async Task<IActionResult> DeleteRegion(Guid id)
        {
            var response = await _staticRepo.DeleteRegion(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("city/{id:guid}")]
        public async Task<IActionResult> DeleteCity(Guid id)
        {
            var response = await _staticRepo.DeleteCity(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}