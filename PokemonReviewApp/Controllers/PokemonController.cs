using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PokemonController : ControllerBase
    {
        private readonly IPokemonRepository _pokemonRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IOwnerRepository _ownerRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PokemonController> _logger;

        public PokemonController(
            IPokemonRepository pokemonRepository,
            IReviewRepository reviewRepository,
            IOwnerRepository ownerRepository,
            ICategoryRepository categoryRepository,
            IMapper mapper,
            ILogger<PokemonController> logger)
        {
            _pokemonRepository = pokemonRepository;
            _reviewRepository = reviewRepository;
            _ownerRepository = ownerRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PokemonDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPokemons()
        {
            try
            {
                var pokemons = await _pokemonRepository.GetPokemonsAsync();
                var pokemonDtos = _mapper.Map<List<PokemonDto>>(pokemons);

                return Ok(pokemonDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving pokemons");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving pokemons");
            }
        }

        [HttpGet("{pokeId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PokemonDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPokemon(int pokeId)
        {
            try
            {
                if (pokeId <= 0)
                    return BadRequest("Invalid Pokemon ID");

                var pokemonExists = await _pokemonRepository.PokemonExistsAsync(pokeId);
                if (!pokemonExists)
                    return NotFound($"Pokemon with ID {pokeId} not found");

                var pokemon = await _pokemonRepository.GetPokemonAsync(pokeId);
                var pokemonDto = _mapper.Map<PokemonDto>(pokemon);

                return Ok(pokemonDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving pokemon with ID {PokeId}", pokeId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the pokemon");
            }
        }

        [HttpGet("{pokeId:int}/rating")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(decimal))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPokemonRating(int pokeId)
        {
            try
            {
                if (pokeId <= 0)
                    return BadRequest("Invalid Pokemon ID");

                var pokemonExists = await _pokemonRepository.PokemonExistsAsync(pokeId);
                if (!pokemonExists)
                    return NotFound($"Pokemon with ID {pokeId} not found");

                var rating = await _pokemonRepository.GetPokemonRatingAsync(pokeId);

                return Ok(rating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving rating for pokemon with ID {PokeId}", pokeId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the pokemon rating");
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreatePokemon(
            [FromQuery] int ownerId, 
            [FromQuery] int catId, 
            [FromBody] PokemonDto pokemonCreate)
        {
            try
            {
                if (pokemonCreate == null)
                    return BadRequest("Pokemon data is required");

                if (ownerId <= 0 || catId <= 0)
                    return BadRequest("Valid owner ID and category ID are required");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if owner exists
                var ownerExists = await _ownerRepository.OwnerExistsAsync(ownerId);
                if (!ownerExists)
                    return NotFound($"Owner with ID {ownerId} not found");

                // Check if category exists
                var categoryExists = await _categoryRepository.CategoryExistsAsync(catId);
                if (!categoryExists)
                    return NotFound($"Category with ID {catId} not found");

                // Check for duplicate pokemon
                var existingPokemon = await _pokemonRepository.GetPokemonTrimToUpperAsync(pokemonCreate);
                if (existingPokemon != null)
                {
                    return Conflict($"Pokemon with name '{pokemonCreate.Name}' already exists");
                }

                var pokemonMap = _mapper.Map<Pokemon>(pokemonCreate);

                var created = await _pokemonRepository.CreatePokemonAsync(ownerId, catId, pokemonMap);
                if (!created)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create pokemon");
                }

                var createdPokemonDto = _mapper.Map<PokemonDto>(pokemonMap);
                return CreatedAtAction(nameof(GetPokemon), new { pokeId = createdPokemonDto.Id }, createdPokemonDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating pokemon");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the pokemon");
            }
        }

        [HttpPut("{pokeId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePokemon(
            int pokeId, 
            [FromQuery] int ownerId, 
            [FromQuery] int catId,
            [FromBody] PokemonDto updatedPokemon)
        {
            try
            {
                if (updatedPokemon == null)
                    return BadRequest("Pokemon data is required");

                if (pokeId != updatedPokemon.Id)
                    return BadRequest("Pokemon ID mismatch");

                if (ownerId <= 0 || catId <= 0)
                    return BadRequest("Valid owner ID and category ID are required");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var pokemonExists = await _pokemonRepository.PokemonExistsAsync(pokeId);
                if (!pokemonExists)
                    return NotFound($"Pokemon with ID {pokeId} not found");

                // Check if owner exists
                var ownerExists = await _ownerRepository.OwnerExistsAsync(ownerId);
                if (!ownerExists)
                    return NotFound($"Owner with ID {ownerId} not found");

                // Check if category exists
                var categoryExists = await _categoryRepository.CategoryExistsAsync(catId);
                if (!categoryExists)
                    return NotFound($"Category with ID {catId} not found");

                // Check for duplicate pokemon name (excluding current pokemon)
                var existingPokemon = await _pokemonRepository.GetPokemonTrimToUpperAsync(updatedPokemon);
                if (existingPokemon != null && existingPokemon.Id != pokeId)
                {
                    return Conflict($"Pokemon with name '{updatedPokemon.Name}' already exists");
                }

                var pokemonMap = _mapper.Map<Pokemon>(updatedPokemon);

                var updated = await _pokemonRepository.UpdatePokemonAsync(ownerId, catId, pokemonMap);
                if (!updated)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update pokemon");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating pokemon with ID {PokeId}", pokeId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the pokemon");
            }
        }

        [HttpDelete("{pokeId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePokemon(int pokeId)
        {
            try
            {
                if (pokeId <= 0)
                    return BadRequest("Invalid Pokemon ID");

                var pokemonExists = await _pokemonRepository.PokemonExistsAsync(pokeId);
                if (!pokemonExists)
                    return NotFound($"Pokemon with ID {pokeId} not found");

                var pokemonToDelete = await _pokemonRepository.GetPokemonAsync(pokeId);
                if (pokemonToDelete == null)
                    return NotFound($"Pokemon with ID {pokeId} not found");

                // Get and delete reviews first
                var reviewsToDelete = await _reviewRepository.GetReviewsOfAPokemonAsync(pokeId);
                if (reviewsToDelete.Any())
                {
                    var reviewsDeleted = await _reviewRepository.DeleteReviewsAsync(reviewsToDelete.ToList());
                    if (!reviewsDeleted)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete associated reviews");
                    }
                }

                // Delete the pokemon
                var pokemonDeleted = await _pokemonRepository.DeletePokemonAsync(pokemonToDelete);
                if (!pokemonDeleted)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete pokemon");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting pokemon with ID {PokeId}", pokeId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the pokemon");
            }
        }
    }
}
