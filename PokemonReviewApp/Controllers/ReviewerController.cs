using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewerController : ControllerBase
    {
        private readonly IReviewerRepository _reviewerRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ReviewerController> _logger;

        public ReviewerController(
            IReviewerRepository reviewerRepository, 
            IReviewRepository reviewRepository,
            IMapper mapper,
            ILogger<ReviewerController> logger)
        {
            _reviewerRepository = reviewerRepository;
            _reviewRepository = reviewRepository;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ReviewerDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetReviewers()
        {
            try
            {
                var reviewers = await _reviewerRepository.GetReviewersAsync();
                var reviewerDtos = _mapper.Map<List<ReviewerDto>>(reviewers);

                return Ok(reviewerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving reviewers");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving reviewers");
            }
        }

        [HttpGet("{reviewerId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ReviewerDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReviewer(int reviewerId)
        {
            try
            {
                if (reviewerId <= 0)
                    return BadRequest("Invalid reviewer ID");

                var reviewerExists = await _reviewerRepository.ReviewerExistsAsync(reviewerId);
                if (!reviewerExists)
                    return NotFound($"Reviewer with ID {reviewerId} not found");

                var reviewer = await _reviewerRepository.GetReviewerAsync(reviewerId);
                var reviewerDto = _mapper.Map<ReviewerDto>(reviewer);

                return Ok(reviewerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving reviewer with ID {ReviewerId}", reviewerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the reviewer");
            }
        }

        [HttpGet("{reviewerId:int}/reviews")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ReviewDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReviewsByAReviewer(int reviewerId)
        {
            try
            {
                if (reviewerId <= 0)
                    return BadRequest("Invalid reviewer ID");

                var reviewerExists = await _reviewerRepository.ReviewerExistsAsync(reviewerId);
                if (!reviewerExists)
                    return NotFound($"Reviewer with ID {reviewerId} not found");

                var reviews = await _reviewerRepository.GetReviewsByReviewerAsync(reviewerId);
                var reviewDtos = _mapper.Map<List<ReviewDto>>(reviews);

                return Ok(reviewDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving reviews for reviewer with ID {ReviewerId}", reviewerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving reviews for the reviewer");
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateReviewer([FromBody] ReviewerDto reviewerCreate)
        {
            try
            {
                if (reviewerCreate == null)
                    return BadRequest("Reviewer data is required");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check for duplicate reviewer by last name (and optionally first name)
                var existingReviewer = await _reviewerRepository.GetReviewerByNameAsync(
                    reviewerCreate.FirstName?.Trim(), 
                    reviewerCreate.LastName.Trim());

                if (existingReviewer != null)
                {
                    return Conflict($"Reviewer '{reviewerCreate.FirstName} {reviewerCreate.LastName}' already exists");
                }

                var reviewerMap = _mapper.Map<Reviewer>(reviewerCreate);

                var created = await _reviewerRepository.CreateReviewerAsync(reviewerMap);
                if (!created)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create reviewer");
                }

                var createdReviewerDto = _mapper.Map<ReviewerDto>(reviewerMap);
                return CreatedAtAction(nameof(GetReviewer), new { reviewerId = createdReviewerDto.Id }, createdReviewerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating reviewer");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the reviewer");
            }
        }

        [HttpPut("{reviewerId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateReviewer(int reviewerId, [FromBody] ReviewerDto updatedReviewer)
        {
            try
            {
                if (updatedReviewer == null)
                    return BadRequest("Reviewer data is required");

                if (reviewerId != updatedReviewer.Id)
                    return BadRequest("Reviewer ID mismatch");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var reviewerExists = await _reviewerRepository.ReviewerExistsAsync(reviewerId);
                if (!reviewerExists)
                    return NotFound($"Reviewer with ID {reviewerId} not found");

                // Check for duplicate reviewer name (excluding current reviewer)
                var existingReviewer = await _reviewerRepository.GetReviewerByNameAsync(
                    updatedReviewer.FirstName?.Trim(), 
                    updatedReviewer.LastName.Trim());

                if (existingReviewer != null && existingReviewer.Id != reviewerId)
                {
                    return Conflict($"Reviewer '{updatedReviewer.FirstName} {updatedReviewer.LastName}' already exists");
                }

                var reviewerMap = _mapper.Map<Reviewer>(updatedReviewer);

                var updated = await _reviewerRepository.UpdateReviewerAsync(reviewerMap);
                if (!updated)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update reviewer");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating reviewer with ID {ReviewerId}", reviewerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the reviewer");
            }
        }

        [HttpDelete("{reviewerId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReviewer(int reviewerId)
        {
            try
            {
                if (reviewerId <= 0)
                    return BadRequest("Invalid reviewer ID");

                var reviewerExists = await _reviewerRepository.ReviewerExistsAsync(reviewerId);
                if (!reviewerExists)
                    return NotFound($"Reviewer with ID {reviewerId} not found");

                var reviewerToDelete = await _reviewerRepository.GetReviewerAsync(reviewerId);
                if (reviewerToDelete == null)
                    return NotFound($"Reviewer with ID {reviewerId} not found");

                // Get and delete associated reviews first
                var reviewsToDelete = await _reviewerRepository.GetReviewsByReviewerAsync(reviewerId);
                if (reviewsToDelete.Any())
                {
                    var reviewsDeleted = await _reviewRepository.DeleteReviewsAsync(reviewsToDelete.ToList());
                    if (!reviewsDeleted)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete associated reviews");
                    }
                }

                // Delete the reviewer
                var reviewerDeleted = await _reviewerRepository.DeleteReviewerAsync(reviewerToDelete);
                if (!reviewerDeleted)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete reviewer");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting reviewer with ID {ReviewerId}", reviewerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the reviewer");
            }
        }
    }
}
