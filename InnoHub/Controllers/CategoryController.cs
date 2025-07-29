using InnoHub.Core.Data;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public CategoryController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet("getAllCategories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _unitOfWork.Category.GetAllAsync();

            var categoryVMs = categories.Select(c => new CategoryViewModel
            {
                CategoryId = c.Id,
                CategoryName = c.Name
            }).ToList();

            return Ok(categoryVMs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _unitOfWork.Category.GetByIdAsync(id);
            if (category == null)
                return NotFound(new { Message = "Category not found." });

            return Ok(new CategoryViewModel { CategoryId = category.Id, CategoryName = category.Name });
        }

        [HttpPost]
        public async Task<IActionResult> AddNewCategory(
      [FromHeader(Name = "Authorization")] string authorizationHeader,
      [FromForm] AddNewCategory categoryVM)
        {
            // ✅ Validate ModelState
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    Message = "Invalid input data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });

            // ✅ Authenticate & Authorize User
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
                return Unauthorized(new { Message = "Only admins can create categories." });

            // ✅ Handle File Upload Correctly
            string imagePath = null;
            if (categoryVM.ImageUrl != null)
            {
                // Validate file types
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".svg" };
                var fileExtension = Path.GetExtension(categoryVM.ImageUrl.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { Message = "Only .jpg, .jpeg, .png, or .svg files are allowed." });

                imagePath = await _unitOfWork.FileService.SaveFileAsync(categoryVM.ImageUrl, "wwwroot/images/categories");
            }

            // ✅ Create Category Object
            var category = new Category
            {
                Name = categoryVM.Name,
                Description = categoryVM.Description,
                ImageUrl = imagePath,
                IsPopular = categoryVM.IsPopular
            };

            // ✅ Save to Database
            await _unitOfWork.Category.AddAsync(category);
            await _unitOfWork.Complete();

            return Ok(new { Message = "Category added successfully." });
        }

        [HttpPatch("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromForm] UpdateCategoryDTO updateDTO)
        {
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
                return Unauthorized(new { Message = "Only admins can update categories." });

            var category = await _unitOfWork.Category.GetByIdAsync(updateDTO.CategoryID);
            if (category == null)
                return NotFound(new { Message = "Category not found." });

            if (updateDTO.ImageUrl != null)
            {
                // Validate file types
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".svg" };
                var fileExtension = Path.GetExtension(updateDTO.ImageUrl.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { Message = "Only .jpg, .jpeg, .png, or .svg files are allowed." });

                _unitOfWork.FileService.DeleteFile(category.ImageUrl);
                category.ImageUrl = await _unitOfWork.FileService.SaveFileAsync(updateDTO.ImageUrl, "wwwroot/images/categories");
            }

            if (!string.IsNullOrWhiteSpace(updateDTO.Name))
                category.Name = updateDTO.Name;

            if (!string.IsNullOrWhiteSpace(updateDTO.Description))
                category.Description = updateDTO.Description;

            if (updateDTO.IsPopular.HasValue)
                category.IsPopular = updateDTO.IsPopular.Value;

            await _unitOfWork.Complete();
            return Ok(new { Message = "Category updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            int id)
        {
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
                return Unauthorized(new { Message = "Only admins can delete categories." });

            var category = await _unitOfWork.Category.GetByIdAsync(id);
            if (category == null)
                return NotFound(new { Message = "Category not found." });

            _unitOfWork.FileService.DeleteFile(category.ImageUrl);

            if (!await _unitOfWork.Category.DeleteAsync(id))
                return BadRequest(new { Message = "Failed to delete the category." });

            await _unitOfWork.Complete();

            return Ok(new { Message = "Category deleted successfully." });
        }
        [HttpGet("getProductsByCategory/{id}")]
        public async Task<IActionResult> GetWithAllProductsAsync(int id)
        {
            // Validate the input
            if (id <= 0)
            {
                return BadRequest(new { Message = "Invalid category ID." });
            }

            // Retrieve the category
            var category = await _unitOfWork.Category.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound(new { Message = "Category not found." });
            }

            // Retrieve products by category ID
            var products = await _unitOfWork.Product.GetAllProductsByCategoryId(id);

            // If no products are found, return the category with an empty product list
            if (products == null || !products.Any())
            {
                return Ok(new
                {
                    CategoryName = category.Name,
                    CategoryDescription = category.Description,
                    Products = new List<AllProductsOnspecificCategoryDTO>()
                });
            }

            // Pre-fetch author names in bulk
            var authorIds = products.Select(p => p.AuthorId).Distinct();
            var authorNames = await _unitOfWork.Category.GetAuthorNamesByIdsAsync(authorIds);

            // Map products to DTO
            var listOfProducts = products.Select(product => new AllProductsOnspecificCategoryDTO
            {
                AuthorName = authorNames.TryGetValue(product.AuthorId, out var authorName) ? authorName : "Unknown Author",
                ProductId = product.Id,
                HomePicture = !string.IsNullOrWhiteSpace(product.HomePicture)
                    ? $"https://innova-hub.premiumasp.net{product.HomePicture}"
                    : null,
                ProductDescription = product.Description ?? "No description available.",
                ProductWeight = product.Weight ?? 0,
                ProductDimensions = product.Dimensions ?? "Not Determined",
                ProductName = product.Name,
                IsAvailable = product.Stock > 0,
                ProductPictures = product.ProductPictures != null
                    ? product.ProductPictures.Select(pic => $"https://innova-hub.premiumasp.net{pic.PictureUrl}").ToList() // ✅ FIXED HERE
                    : new List<string>(),
                ProductPrice = product.Price,
                Stock = product.Stock
            }).ToList();

            // Return the category and products
            return Ok(new CategoryNameWithAllProducts
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                CategoryDescription = category.Description,
                AllProductsOnspecificCategories = listOfProducts
            });
        }

        [HttpGet("getPopularCategories")]
        public async Task<IActionResult> GetPopularCategories()
        {
            var categories = await _unitOfWork.Category.GetAllAsync();

            var popularCategories = (categories ?? Enumerable.Empty<Category>())
                .Where(c => c.IsPopular)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    ImageUrl = $"https://innova-hub.premiumasp.net{c.ImageUrl}"
                })
                .ToList();

            return Ok(popularCategories);
        }
    }
}
