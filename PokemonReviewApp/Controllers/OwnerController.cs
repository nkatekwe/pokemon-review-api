using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OwnerController : ControllerBase
    {
        private readonly IOwnerRepository _ownerRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<OwnerController> _logger;

        public OwnerController(
            IOwnerRepository ownerRepository, 
            ICountryRepository countryRepository,
            IMapper mapper,
            ILogger<OwnerController> logger)
        {
            _ownerRepository = ownerRepository;
            _countryRepository = countryRepository;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OwnerDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetOwners()
        {
            try
            {
                var owners = await _ownerRepository.GetOwnersAsync();
                var ownerDtos = _mapper.Map<List<OwnerDto>>(owners);

                return Ok(ownerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving owners");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving owners");
            }
        }

        [HttpGet("{ownerId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OwnerDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOwner(int ownerId)
        {
            try
            {
                if (ownerId <= 0)
                    return BadRequest("Invalid owner ID");

                var ownerExists = await _ownerRepository.OwnerExistsAsync(ownerId);
                if (!ownerExists)
                    return NotFound($"Owner with ID {ownerId} not found");

                var owner = await _ownerRepository.GetOwnerAsync(ownerId);
                var ownerDto = _mapper.Map<OwnerDto>(owner);

                return Ok(ownerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving owner with ID {OwnerId}", ownerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the owner");
            }
        }

        [HttpGet("{ownerId:int}/pokemon")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PokemonDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPokemonByOwner(int ownerId)
        {
            try
            {
                if (ownerId <= 0)
                    return BadRequest("Invalid owner ID");

                var ownerExists = await _ownerRepository.OwnerExistsAsync(ownerId);
                if (!ownerExists)
                    return NotFound($"Owner with ID {ownerId} not found");

                var pokemon = await _ownerRepository.GetPokemonByOwnerAsync(ownerId);
                var pokemonDtos = _mapper.Map<List<PokemonDto>>(pokemon);

                return Ok(pokemonDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving pokemon for owner with ID {OwnerId}", ownerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving pokemon for the owner");
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateOwner([FromQuery] int countryId, [FromBody] OwnerDto ownerCreate)
        {
            try
            {
                if (ownerCreate == null)
                    return BadRequest("Owner data is required");

                if (countryId <= 0)
                    return BadRequest("Valid country ID is required");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if country exists
                var countryExists = await _countryRepository.CountryExistsAsync(countryId);
                if (!countryExists)
                    return NotFound($"Country with ID {countryId} not found");

                // Check if owner already exists (by last name and other criteria)
                var existingOwner = await _ownerRepository.GetOwnerByNameAsync(ownerCreate.LastName.Trim());
                if (existingOwner != null)
                {
                    // Additional check: compare first name and other properties if needed
                    if (string.Equals(existingOwner.FirstName?.Trim(), ownerCreate.FirstName?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return Conflict($"Owner '{ownerCreate.FirstName} {ownerCreate.LastName}' already exists");
                    }
                }

                var ownerMap = _mapper.Map<Owner>(ownerCreate);
                ownerMap.Country = await _countryRepository.GetCountryAsync(countryId);

                if (ownerMap.Country == null)
                    return NotFound($"Country with ID {countryId} not found");

                var created = await _ownerRepository.CreateOwnerAsync(ownerMap);
                if (!created)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create owner");
                }

                var createdOwnerDto = _mapper.Map<OwnerDto>(ownerMap);
                return CreatedAtAction(nameof(GetOwner), new { ownerId = createdOwnerDto.Id }, createdOwnerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating owner");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the owner");
            }
        }

        [HttpPut("{ownerId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOwner(int ownerId, [FromBody] OwnerDto updatedOwner)
        {
            try
            {
                if (updatedOwner == null)
                    return BadRequest("Owner data is required");

                if (ownerId != updatedOwner.Id)
                    return BadRequest("Owner ID mismatch");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var ownerExists = await _ownerRepository.OwnerExistsAsync(ownerId);
                if (!ownerExists)
                    return NotFound($"Owner with ID {ownerId} not found");

                // Check for duplicate owner with same name (excluding current owner)
                var existingOwner = await _ownerRepository.GetOwnerByNameAsync(updatedOwner.LastName.Trim());
                if (existingOwner != null && existingOwner.Id != ownerId)
                {
                    if (string.Equals(existingOwner.FirstName?.Trim(), updatedOwner.FirstName?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return Conflict($"Owner '{updatedOwner.FirstName} {updatedOwner.LastName}' already exists");
                    }
                }

                var ownerMap = _mapper.Map<Owner>(updatedOwner);

                var updated = await _ownerRepository.UpdateOwnerAsync(ownerMap);
                if (!updated)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update owner");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating owner with ID {OwnerId}", ownerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the owner");
            }
        }

        [HttpDelete("{ownerId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOwner(int ownerId)
        {
            try
            {
                if (ownerId <= 0)
                    return BadRequest("Invalid owner ID");

                var ownerExists = await _ownerRepository.OwnerExistsAsync(ownerId);
                if (!ownerExists)
                    return NotFound($"Owner with ID {ownerId} not found");

                var ownerToDelete = await _ownerRepository.GetOwnerAsync(ownerId);
                if (ownerToDelete == null)
                    return NotFound($"Owner with ID {ownerId} not found");

                var deleted = await _ownerRepository.DeleteOwnerAsync(ownerToDelete);
                if (!deleted)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete owner");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting owner with ID {OwnerId}", ownerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the owner");
            }
        }
    }
}
