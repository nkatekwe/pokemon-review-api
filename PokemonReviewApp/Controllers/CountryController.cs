using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CountryController> _logger;

        public CountryController(
            ICountryRepository countryRepository, 
            IMapper mapper,
            ILogger<CountryController> logger)
        {
            _countryRepository = countryRepository;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CountryDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCountries()
        {
            try
            {
                var countries = await _countryRepository.GetCountriesAsync();
                var countryDtos = _mapper.Map<List<CountryDto>>(countries);

                return Ok(countryDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving countries");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving countries");
            }
        }

        [HttpGet("{countryId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CountryDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCountry(int countryId)
        {
            try
            {
                if (countryId <= 0)
                    return BadRequest("Invalid country ID");

                var countryExists = await _countryRepository.CountryExistsAsync(countryId);
                if (!countryExists)
                    return NotFound($"Country with ID {countryId} not found");

                var country = await _countryRepository.GetCountryAsync(countryId);
                var countryDto = _mapper.Map<CountryDto>(country);

                return Ok(countryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving country with ID {CountryId}", countryId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the country");
            }
        }

        [HttpGet("owners/{ownerId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CountryDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCountryOfAnOwner(int ownerId)
        {
            try
            {
                if (ownerId <= 0)
                    return BadRequest("Invalid owner ID");

                var country = await _countryRepository.GetCountryByOwnerAsync(ownerId);
                if (country == null)
                    return NotFound($"Country for owner with ID {ownerId} not found");

                var countryDto = _mapper.Map<CountryDto>(country);

                return Ok(countryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving country for owner with ID {OwnerId}", ownerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the country");
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCountry([FromBody] CountryDto countryCreate)
        {
            try
            {
                if (countryCreate == null)
                    return BadRequest("Country data is required");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if country already exists
                var existingCountry = await _countryRepository.GetCountryByNameAsync(countryCreate.Name.Trim());
                if (existingCountry != null)
                {
                    return Conflict($"Country with name '{countryCreate.Name}' already exists");
                }

                var countryMap = _mapper.Map<Country>(countryCreate);

                var created = await _countryRepository.CreateCountryAsync(countryMap);
                if (!created)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create country");
                }

                var createdCountryDto = _mapper.Map<CountryDto>(countryMap);
                return CreatedAtAction(nameof(GetCountry), new { countryId = createdCountryDto.Id }, createdCountryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating country");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the country");
            }
        }

        [HttpPut("{countryId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCountry(int countryId, [FromBody] CountryDto updatedCountry)
        {
            try
            {
                if (updatedCountry == null)
                    return BadRequest("Country data is required");

                if (countryId != updatedCountry.Id)
                    return BadRequest("Country ID mismatch");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var countryExists = await _countryRepository.CountryExistsAsync(countryId);
                if (!countryExists)
                    return NotFound($"Country with ID {countryId} not found");

                // Check for duplicate name with other countries
                var existingCountry = await _countryRepository.GetCountryByNameAsync(updatedCountry.Name.Trim());
                if (existingCountry != null && existingCountry.Id != countryId)
                {
                    return Conflict($"Country with name '{updatedCountry.Name}' already exists");
                }

                var countryMap = _mapper.Map<Country>(updatedCountry);

                var updated = await _countryRepository.UpdateCountryAsync(countryMap);
                if (!updated)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update country");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating country with ID {CountryId}", countryId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the country");
            }
        }

        [HttpDelete("{countryId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCountry(int countryId)
        {
            try
            {
                if (countryId <= 0)
                    return BadRequest("Invalid country ID");

                var countryExists = await _countryRepository.CountryExistsAsync(countryId);
                if (!countryExists)
                    return NotFound($"Country with ID {countryId} not found");

                var countryToDelete = await _countryRepository.GetCountryAsync(countryId);
                if (countryToDelete == null)
                    return NotFound($"Country with ID {countryId} not found");

                var deleted = await _countryRepository.DeleteCountryAsync(countryToDelete);
                if (!deleted)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete country");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting country with ID {CountryId}", countryId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the country");
            }
        }
    }
}
