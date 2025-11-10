using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;

namespace PokemonReviewApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            ICategoryRepository categoryRepository, 
            IMapper mapper,
            ILogger<CategoryController> logger)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CategoryDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _categoryRepository.GetCategoriesAsync();
                var categoryDtos = _mapper.Map<List<CategoryDto>>(categories);

                return Ok(categoryDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving categories");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving categories");
            }
        }

        [HttpGet("{categoryId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategory(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                    return BadRequest("Invalid category ID");

                var categoryExists = await _categoryRepository.CategoryExistsAsync(categoryId);
                if (!categoryExists)
                    return NotFound($"Category with ID {categoryId} not found");

                var category = await _categoryRepository.GetCategoryAsync(categoryId);
                var categoryDto = _mapper.Map<CategoryDto>(category);

                return Ok(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving category with ID {CategoryId}", categoryId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the category");
            }
        }

        [HttpGet("{categoryId:int}/pokemon")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PokemonDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPokemonByCategoryId(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                    return BadRequest("Invalid category ID");

                var categoryExists = await _categoryRepository.CategoryExistsAsync(categoryId);
                if (!categoryExists)
                    return NotFound($"Category with ID {categoryId} not found");

                var pokemons = await _categoryRepository.GetPokemonByCategoryAsync(categoryId);
                var pokemonDtos = _mapper.Map<List<PokemonDto>>(pokemons);

                return Ok(pokemonDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving pokemon for category ID {CategoryId}", categoryId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving pokemon");
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto categoryCreate)
        {
            try
            {
                if (categoryCreate == null)
                    return BadRequest("Category data is required");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check for duplicate category name
                var existingCategory = await _categoryRepository.GetCategoryByNameAsync(categoryCreate.Name.Trim());
                if (existingCategory != null)
                {
                    return Conflict($"Category with name '{categoryCreate.Name}' already exists");
                }

                var category = _mapper.Map<Category>(categoryCreate);
                
                var created = await _categoryRepository.CreateCategoryAsync(category);
                if (!created)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create category");
                }

                var createdCategoryDto = _mapper.Map<CategoryDto>(category);
                
                return CreatedAtAction(
                    nameof(GetCategory), 
                    new { categoryId = category.Id }, 
                    createdCategoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating category");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the category");
            }
        }

        [HttpPut("{categoryId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(int categoryId, [FromBody] CategoryUpdateDto updatedCategory)
        {
            try
            {
                if (updatedCategory == null)
                    return BadRequest("Category data is required");

                if (categoryId <= 0)
                    return BadRequest("Invalid category ID");

                if (categoryId != updatedCategory.Id)
                    return BadRequest("Category ID mismatch");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var categoryExists = await _categoryRepository.CategoryExistsAsync(categoryId);
                if (!categoryExists)
                    return NotFound($"Category with ID {categoryId} not found");

                // Check for duplicate name with other categories
                var existingCategory = await _categoryRepository.GetCategoryByNameAsync(updatedCategory.Name.Trim());
                if (existingCategory != null && existingCategory.Id != categoryId)
                {
                    return Conflict($"Category with name '{updatedCategory.Name}' already exists");
                }

                var category = _mapper.Map<Category>(updatedCategory);

                var updated = await _categoryRepository.UpdateCategoryAsync(category);
                if (!updated)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update category");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating category with ID {CategoryId}", categoryId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the category");
            }
        }

        [HttpDelete("{categoryId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                    return BadRequest("Invalid category ID");

                var categoryExists = await _categoryRepository.CategoryExistsAsync(categoryId);
                if (!categoryExists)
                    return NotFound($"Category with ID {categoryId} not found");

                var categoryToDelete = await _categoryRepository.GetCategoryAsync(categoryId);
                if (categoryToDelete == null)
                    return NotFound($"Category with ID {categoryId} not found");

                var deleted = await _categoryRepository.DeleteCategoryAsync(categoryToDelete);
                if (!deleted)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete category");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category with ID {CategoryId}", categoryId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the category");
            }
        }
    }
}
