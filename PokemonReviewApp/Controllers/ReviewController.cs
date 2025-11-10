using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapper _mapper;
        private readonly IReviewerRepository _reviewerRepository;
        private readonly IPokemonRepository _pokemonRepository;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            IReviewRepository reviewRepository, 
            IMapper mapper,
            IPokemonRepository pokemonRepository,
            IReviewerRepository reviewerRepository,
            ILogger<ReviewController> logger)
        {
            _reviewRepository = reviewRepository;
            _mapper = mapper;
            _reviewerRepository = reviewerRepository;
            _pokemonRepository = pokemonRepository;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ReviewDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetReviews()
        {
            try
            {
                var reviews = await _reviewRepository.GetReviewsAsync();
                var reviewDtos = _mapper.Map<List<ReviewDto>>(reviews);

                return Ok(reviewDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving reviews");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving reviews");
            }
        }

        [HttpGet("{reviewId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ReviewDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReview(int reviewId)
        {
            try
            {
                if (reviewId <= 0)
                    return BadRequest("Invalid review ID");

                var reviewExists = await _reviewRepository.ReviewExistsAsync(reviewId);
                if (!reviewExists)
                    return NotFound($"Review with ID {reviewId} not found");

                var review = await _reviewRepository.GetReviewAsync(reviewId);
                var reviewDto = _mapper.Map<ReviewDto>(review);

                return Ok(reviewDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving review with ID {ReviewId}", reviewId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the review");
            }
        }

        [HttpGet("pokemon/{pokeId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ReviewDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReviewsForAPokemon(int pokeId)
        {
            try
            {
                if (pokeId <= 0)
                    return BadRequest("Invalid Pokemon ID");

                var pokemonExists = await _pokemonRepository.PokemonExistsAsync(pokeId);
                if (!pokemonExists)
                    return NotFound($"Pokemon with ID {pokeId} not found");

                var reviews = await _reviewRepository.GetReviewsOfAPokemonAsync(pokeId);
                var reviewDtos = _mapper.Map<List<ReviewDto>>(reviews);

                return Ok(reviewDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving reviews for pokemon with ID {PokeId}", pokeId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving reviews for the pokemon");
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateReview(
            [FromQuery] int reviewerId, 
            [FromQuery] int pokeId, 
            [FromBody] ReviewDto reviewCreate)
        {
            try
            {
                if (reviewCreate == null)
                    return BadRequest("Review data is required");

                if (reviewerId <= 0 || pokeId <= 0)
                    return BadRequest("Valid reviewer ID and Pokemon ID are required");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if reviewer exists
                var reviewerExists = await _reviewerRepository.ReviewerExistsAsync(reviewerId);
                if (!reviewerExists)
                    return NotFound($"Reviewer with ID {reviewerId} not found");

                // Check if pokemon exists
                var pokemonExists = await _pokemonRepository.PokemonExistsAsync(pokeId);
                if (!pokemonExists)
                    return NotFound($"Pokemon with ID {pokeId} not found");

                // Check for duplicate review title
                var existingReview = await _reviewRepository.GetReviewByTitleAsync(reviewCreate.Title.Trim());
                if (existingReview != null)
                {
                    return Conflict($"Review with title '{reviewCreate.Title}' already exists");
                }

                var reviewMap = _mapper.Map<Review>(reviewCreate);

                // Set related entities
                reviewMap.Pokemon = await _pokemonRepository.GetPokemonAsync(pokeId);
                reviewMap.Reviewer = await _reviewerRepository.GetReviewerAsync(reviewerId);

                if (reviewMap.Pokemon == null)
                    return NotFound($"Pokemon with ID {pokeId} not found");

                if (reviewMap.Reviewer == null)
                    return NotFound($"Reviewer with ID {reviewerId} not found");

                var created = await _reviewRepository.CreateReviewAsync(reviewMap);
                if (!created)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create review");
                }

                var createdReviewDto = _mapper.Map<ReviewDto>(reviewMap);
                return CreatedAtAction(nameof(GetReview), new { reviewId = createdReviewDto.Id }, createdReviewDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating review");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the review");
            }
        }

        [HttpPut("{reviewId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] ReviewDto updatedReview)
        {
            try
            {
                if (updatedReview == null)
                    return BadRequest("Review data is required");

                if (reviewId != updatedReview.Id)
                    return BadRequest("Review ID mismatch");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var reviewExists = await _reviewRepository.ReviewExistsAsync(reviewId);
                if (!reviewExists)
                    return NotFound($"Review with ID {reviewId} not found");

                // Check for duplicate review title (excluding current review)
                var existingReview = await _reviewRepository.GetReviewByTitleAsync(updatedReview.Title.Trim());
                if (existingReview != null && existingReview.Id != reviewId)
                {
                    return Conflict($"Review with title '{updatedReview.Title}' already exists");
                }

                var reviewMap = _mapper.Map<Review>(updatedReview);

                var updated = await _reviewRepository.UpdateReviewAsync(reviewMap);
                if (!updated)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update review");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating review with ID {ReviewId}", reviewId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the review");
            }
        }

        [HttpDelete("{reviewId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            try
            {
                if (reviewId <= 0)
                    return BadRequest("Invalid review ID");

                var reviewExists = await _reviewRepository.ReviewExistsAsync(reviewId);
                if (!reviewExists)
                    return NotFound($"Review with ID {reviewId} not found");

                var reviewToDelete = await _reviewRepository.GetReviewAsync(reviewId);
                if (reviewToDelete == null)
                    return NotFound($"Review with ID {reviewId} not found");

                var deleted = await _reviewRepository.DeleteReviewAsync(reviewToDelete);
                if (!deleted)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete review");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting review with ID {ReviewId}", reviewId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the review");
            }
        }
        
        [HttpDelete("reviewer/{reviewerId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReviewsByReviewer(int reviewerId)
        {
            try
            {
                if (reviewerId <= 0)
                    return BadRequest("Invalid reviewer ID");

                var reviewerExists = await _reviewerRepository.ReviewerExistsAsync(reviewerId);
                if (!reviewerExists)
                    return NotFound($"Reviewer with ID {reviewerId} not found");

                var reviewsToDelete = (await _reviewerRepository.GetReviewsByReviewerAsync(reviewerId)).ToList();
                
                if (reviewsToDelete.Any())
                {
                    var deleted = await _reviewRepository.DeleteReviewsAsync(reviewsToDelete);
                    if (!deleted)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete reviews");
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting reviews for reviewer with ID {ReviewerId}", reviewerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting reviews for the reviewer");
            }
        }
    }
}
