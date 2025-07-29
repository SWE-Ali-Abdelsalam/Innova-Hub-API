using AutoMapper;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UserManager<AppUser> _userManager;

    public ProductController(IUnitOfWork unitOfWork, IMapper mapper, UserManager<AppUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userManager = userManager;
    }

    [HttpGet("getAllProducts")]
    public async Task<IActionResult> GetAllProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page and pageSize must be greater than zero." });
        }

        // Get total product count (no approval filter anymore)
        var totalProducts = await _unitOfWork.Product.CountAsync();

        // Get paginated products (all products regardless of IsApproved)
        var products = await _unitOfWork.Product.GetPaginatedAsync(
            page,
            pageSize,
            orderBy: "Id",
            descending: true,
            includes: new List<Expression<Func<Product, object>>>
            {
            p => p.Author,
            p => p.ProductPictures,
            p => p.Sizes,
            p => p.Colors,
            p => p.Ratings
            }
        // Removed the filter
        );

        var productResponses = products.Select(product =>
        {
            var ratingBreakdown = new Dictionary<int, int> { [1] = 0, [2] = 0, [3] = 0, [4] = 0, [5] = 0 };
            int totalRatings = product.Ratings?.Count ?? 0;

            if (product.Ratings != null)
            {
                foreach (var rating in product.Ratings)
                {
                    if (rating.RatingValue >= 1 && rating.RatingValue <= 5)
                    {
                        ratingBreakdown[rating.RatingValue]++;
                    }
                }
            }

            var ratingPercentages = ratingBreakdown.ToDictionary(
                pair => $"{pair.Key} star",
                pair => totalRatings > 0
                    ? Math.Round((double)pair.Value / totalRatings, 2)
                    : 0
            );

            return new
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductAuthorId = product.AuthorId,
                ProductAuthorName = product.Author != null
                    ? $"{product.Author.FirstName} {product.Author.LastName}"
                    : "Unknown",
                ProductHomePicture = $"https://innova-hub.premiumasp.net{product.HomePicture}",
                ProductPictures = product.ProductPictures?.Select(p => $"https://innova-hub.premiumasp.net{p.PictureUrl}").ToList() ?? new List<string>(),
                ProductDescription = product.Description,
                ProductSizes = product.Sizes?.Select(s => s.SizeName).ToList() ?? new List<string>(),
                ProductColors = product.Colors?.Select(c => c.ColorName).ToList() ?? new List<string>(),
                ProductPriceBeforeDiscount = product.Price,
                ProductPriceAfterDiscount = product.Discount > 0
                    ? product.Price * (1 - product.Discount / 100)
                    : product.Price,
                ProductStock = product.Stock,
                ProductRate = product.AverageRating,
                NumberOfRatings = totalRatings,
                RatingBreakdown = ratingPercentages
            };
        }).ToList();

        var metadata = new
        {
            TotalProducts = totalProducts,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalProducts / (double)pageSize)
        };

        return Ok(new
        {
            Metadata = metadata,
            Products = productResponses
        });
    }

    [HttpGet("getOneProduct/{id}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { Message = "Invalid product ID. The ID must be greater than zero." });
        }

        var product = await _unitOfWork.Product.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound(new { Message = "Product not found." });
        }

        string authorName = product.Author != null
            ? $"{product.Author.FirstName} {product.Author.LastName}"
            : "Unknown Author";

        string categoryName = product.Category?.Name ?? "Unknown Category";

        string homePictureUrl = !string.IsNullOrWhiteSpace(product.HomePicture)
            ? $"https://innova-hub.premiumasp.net{product.HomePicture}"
            : null;

        // Mapping ProductPictures to URLs
        List<string> picturesUrls = product.ProductPictures?
            .Select(pic => $"https://innova-hub.premiumasp.net{pic.PictureUrl}")
            .ToList() ?? new List<string>();

        double averageRating = (product.Ratings != null && product.Ratings.Any())
            ? Math.Round(product.Ratings.Average(r => r.RatingValue), 2)
            : 0;

        int totalRatings = product.Ratings?.Count ?? 0;

        // Calculate rating breakdown percentages
        var ratingBreakdown = new Dictionary<int, int>();
        for (int i = 1; i <= 5; i++)
        {
            ratingBreakdown[i] = 0;
        }

        if (product.Ratings != null && product.Ratings.Any())
        {
            foreach (var rating in product.Ratings)
            {
                if (rating.RatingValue >= 1 && rating.RatingValue <= 5)
                {
                    ratingBreakdown[(int)rating.RatingValue]++;
                }
            }
        }

        // Calculate percentages
        var ratingPercentages = new Dictionary<string, double>();
        foreach (var pair in ratingBreakdown)
        {
            double percentage = totalRatings > 0
                ? Math.Round((double)pair.Value / totalRatings, 2)
                : 0;
            ratingPercentages[$"{pair.Key} star"] = percentage;
        }

        // Map Sizes and Colors
        List<string> productSizes = product.Sizes?.Select(s => s.SizeName).ToList() ?? new List<string>();
        List<string> productColors = product.Colors?.Select(c => c.ColorName).ToList() ?? new List<string>();

        var productReviews = product.Comments?
            .Select(comment => new
            {
                CommentId = comment.Id,
                ReviewerName = $"{comment.User.FirstName} {comment.User.LastName}",
                CommentText = comment.CommentText,
                DatePosted = comment.CreatedAt
            })
            .ToList() ;  // Safe fallback to empty list

        var productViewModel = new
        {
            ProductId = product.Id,
            ProductName = product.Name,
            AuthorId = product.AuthorId,
            AuthorName = authorName,
            CategoryId = product.CategoryId,
            CategoryName = categoryName,
            Description = product.Description ?? "No description available.",
            Weight = product.Weight,
            Dimensions = product.Dimensions,
            HomePicture = homePictureUrl,
            Pictures = picturesUrls,
            PriceBeforeDiscount = product.Price,
            PriceAfterDiscount = product.Discount > 0
            ? product.Price * (1 - product.Discount / 100)
            : product.Price,
            DiscountPercentage = product.Discount,
            Stock = Math.Max(product.Stock, 0),
            AverageRating = averageRating,
            NumberOfRatings = totalRatings,
            RatingBreakdown = ratingPercentages,
            ProductSizes = productSizes,
            ProductColors = productColors,
            NumberOfReviews = productReviews.Count,
            ProductReviews = productReviews
        };

        return Ok(productViewModel);
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct(
        [FromHeader(Name = "Authorization")] string authorizationHeader,
        [FromForm] AddNewProduct productVM)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                Message = "Invalid product data.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        // ✅ Authenticate & Authorize User
        var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "BusinessOwner");
        if (currentUser == null)
        {
            return Unauthorized(new { Message = "Only business owners can add products." });
        }

        // ✅ Check if the category exists
        var category = await _unitOfWork.Category.GetByIdAsync(productVM.CategoryId);
        if (category == null)
        {
            return NotFound(new { Message = "The specified category does not exist." });
        }

        // ✅ Ensure directory for images exists
        var folderPath = _unitOfWork.FileService.EnsureDirectory("wwwroot/images/products");

        string homePicturePath = null;
        List<ProductPicture> productPictures = new List<ProductPicture>();  // Use ProductPicture objects instead of strings

        try
        {
            // ✅ Save Home Picture (if exists)
            if (productVM.HomePicture != null)
            {
                // Validate file types
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".svg" };
                var fileExtension = Path.GetExtension(productVM.HomePicture.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { Message = "Only .jpg, .jpeg, .png, or .svg files are allowed." });

                homePicturePath = await _unitOfWork.FileService.SaveFileAsync(productVM.HomePicture, folderPath);
            }

            // ✅ Save Additional Product Pictures (if any)
            if (productVM.Pictures?.Any() == true)
            {
                var savedPicturePaths = await _unitOfWork.FileService.SaveFilesAsync(productVM.Pictures, folderPath);
                productPictures.AddRange(savedPicturePaths.Select(path => new ProductPicture
                {
                    PictureUrl = path  // You can store file path or URL here
                }));
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error while saving images.", Error = ex.Message });
        }

        // ✅ Create Product Entity
        var product = new Product
        {
            Name = productVM.ProductName,
            AuthorId = currentUser.Id,
            CategoryId = productVM.CategoryId,
            Description = productVM.Description,
            Discount = productVM.Discount,
            HomePicture = homePicturePath,  // Use file path here
            Price = productVM.Price,
            Stock = productVM.Stock,
            Dimensions = productVM.Dimensions,
            Weight = productVM.Weight,
            ProductPictures = productPictures,
            Sizes = productVM.SizeNames.Select(sizeName => new ProductSize { SizeName = sizeName }).ToList(),
            Colors = productVM.ColorNames.Select(colorName => new ProductColor { ColorName = colorName }).ToList(),
            
        };

        // ✅ Add Product to Database
        await _unitOfWork.Product.AddAsync(product);
        await _unitOfWork.Complete();

        return Ok(new { Message = "Product added successfully.", ProductId = product.Id });
    }


    [HttpPatch("UpdateProduct")]
    public async Task<IActionResult> UpdateProduct(
       [FromHeader(Name = "Authorization")] string authorizationHeader,
       [FromForm] UpdateProductDTO updateDTO)
    {
        var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { Message = "Invalid token or user not found." });

        // ✅ Retrieve Product (Check if the product exists)
        var product = await _unitOfWork.Product.GetByIdAsync(updateDTO.ProductId);
        if (product == null)
            return NotFound(new { Message = "Product not found." });

        // ✅ Authenticate User (Check if the user is a Business Owner or Admin)
        if (product.AuthorId != userId && !await _unitOfWork.Auth.IsAdmin(userId))
        {
            return Unauthorized(new { Message = "Only the business owner or admins can update this product." });
        }

        // ✅ Ensure the product belongs to the current user (check if the user is the owner of the product)

        // ✅ Validate Category (Check if the category exists)
        if (updateDTO.CategoryId.HasValue)
        {
            var category = await _unitOfWork.Category.GetByIdAsync(updateDTO.CategoryId.Value);

            if (category == null)
                return NotFound(new { Message = "The specified category does not exist." });
        }

        // ✅ Handle File Uploads (Pictures)
        var folderPath = _unitOfWork.FileService.EnsureDirectory("wwwroot/images/products");

        try
        {
            // 🔹 Update Home Picture (if provided)
            if (updateDTO.Homepicture != null)
            {
                // Validate file types
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".svg" };
                var fileExtension = Path.GetExtension(updateDTO.Homepicture.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { Message = "Only .jpg, .jpeg, .png, or .svg files are allowed." });

                var newHomePicturePath = await _unitOfWork.FileService.SaveFileAsync(updateDTO.Homepicture, folderPath);
                if (!string.IsNullOrEmpty(product.HomePicture))
                    _unitOfWork.FileService.DeleteFile(product.HomePicture); // Delete old home picture
                product.HomePicture = newHomePicturePath; // Assign the new picture path
            }

            // 🔹 Update Product Pictures (if provided)
            if (updateDTO.Pictures != null && updateDTO.Pictures.Count > 0)
            {
                // 1️⃣ Delete old pictures from the database
                if (product.ProductPictures != null && product.ProductPictures.Any())
                {
                    foreach (var pic in product.ProductPictures)
                        _unitOfWork.FileService.DeleteFile(pic.PictureUrl); // Delete old pictures using PictureUrl

                    await _unitOfWork.Product.RemoveAllPicturesByProductIdAsync(product.Id); // Remove from database
                }

                // 2️⃣ Save new pictures
                var savedPicturePaths = await _unitOfWork.FileService.SaveFilesAsync(updateDTO.Pictures, folderPath);
                // Create new ProductPicture objects using PictureUrl
                product.ProductPictures = savedPicturePaths.Select(path => new ProductPicture { PictureUrl = path }).ToList(); // Using PictureUrl here
            }

            // ✅ Update Product Fields
            if (!string.IsNullOrWhiteSpace(updateDTO.ProductName))
                product.Name = updateDTO.ProductName;

            if (!string.IsNullOrWhiteSpace(updateDTO.Description))
                product.Description = updateDTO.Description;

            product.Price = updateDTO.Price ?? product.Price;
            product.Stock = updateDTO.Stock ?? product.Stock;
            product.Dimensions = updateDTO.Dimensions ?? product.Dimensions;
            product.Weight = updateDTO.Weight ?? product.Weight;
            product.Discount = updateDTO.Discount ?? product.Discount;
            product.CategoryId = updateDTO.CategoryId ?? product.CategoryId;

            // ✅ Update Sizes (if provided)
            if (updateDTO.SizeNames != null && updateDTO.SizeNames.Any())
            {
                // Convert List<string> to List<ProductSize>
                product.Sizes = updateDTO.SizeNames.Select(size => new ProductSize { SizeName = size }).ToList();
            }

            // ✅ Update Colors (if provided)
            if (updateDTO.ColorNames != null && updateDTO.ColorNames.Any())
            {
                // Convert List<string> to List<ProductColor>
                product.Colors = updateDTO.ColorNames.Select(color => new ProductColor { ColorName = color }).ToList();
            }

            // ✅ Save Changes to Product
            var isUpdated = await _unitOfWork.Product.UpdateProductAsync(product);
            if (!isUpdated)
                return BadRequest(new { Message = "Failed to update the product." });

            await _unitOfWork.Complete(); // Commit all changes

            return Ok(new { Message = "Product updated successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error updating product.", Error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(
     [FromHeader(Name = "Authorization")] string authorizationHeader, int id)
    {
        var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { Message = "Invalid token or user not found." });

        // ✅ Retrieve the product by its ID
        var product = await _unitOfWork.Product.GetByIdAsync(id);
        if (product == null)
            return NotFound(new { Message = "Product not found." });

        // ✅ Authenticate the user (Check if the user is either Business Owner or Admin)
        if (product.AuthorId != userId && !await _unitOfWork.Auth.IsAdmin(userId))
        {
            return Unauthorized(new { Message = "Only the business owner or admins can delete this product." });
        }

        // ✅ Delete product images BEFORE deleting the product
        try
        {
            // Delete home picture if it exists
            if (!string.IsNullOrEmpty(product.HomePicture))
            {
                _unitOfWork.FileService.DeleteFile(product.HomePicture); // Delete home picture
            }

            // Delete all associated product pictures if they exist
            if (product.ProductPictures != null && product.ProductPictures.Any())
            {
                foreach (var pic in product.ProductPictures)
                {
                    if (!string.IsNullOrEmpty(pic.PictureUrl))
                    {
                        _unitOfWork.FileService.DeleteFile(pic.PictureUrl); // Delete picture file using its URL
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Return a detailed error message for image deletion failure
            return StatusCode(500, new { Message = "Error deleting images.", Error = ex.Message });
        }

        // ✅ Delete the product from the database
        var isDeleted = await _unitOfWork.Product.DeleteAsync(id);
        if (!isDeleted)
            return BadRequest(new { Message = "Failed to delete the product." });

        // ✅ Commit changes to the database
        await _unitOfWork.Complete();

        // ✅ Return success message
        return Ok(new { Message = "Product deleted successfully." });
    }

    [HttpGet("productsSearchFilter")]
    public async Task<IActionResult> ProductsSearchFilter([FromQuery] string name = null, [FromQuery] decimal? from = null, [FromQuery] decimal? to = null, [FromQuery] string location = null)
    {
        var products = await _unitOfWork.Product.GetAllAsync();

        if (!string.IsNullOrEmpty(name))
            products = products.Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (from.HasValue)
            products = products.Where(p => p.Price >= from.Value);

        if (to.HasValue)
            products = products.Where(p => p.Price <= to.Value);

        if (!string.IsNullOrEmpty(location))
            products = products.Where(p => p.Author.City.Contains(location, StringComparison.OrdinalIgnoreCase));

        var result = products.Select(p => new
        {
            ProductName = p.Name,
            ProductAuthor = $"{p.Author.FirstName} {p.Author.LastName}",
            ProductPriceBeforeDiscount = p.Price,
            ProductPriceAfterDiscount = p.Price * (1 - p.Discount / 100),
            ProductStatus = p.Stock > 0 ? $"In stock ({p.Stock})" : "Out of stock"
        });

        return Ok(result);
    }

    [HttpPost("rateAndComment")]
    public async Task<IActionResult> RateAndComment(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    [FromBody] RateAndCommentDTO dto)
    {
        var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { Message = "Invalid authorization token." });

        var product = await _unitOfWork.Product.GetByIdAsync(dto.ProductId);
        if (product == null)
            return NotFound(new { Message = "Product not found." });

        var existingRating = await _unitOfWork.ProductRating
            .GetRatingByProductIdAndUserIdAsync(dto.ProductId, userId);

        if (existingRating != null)
        {
            existingRating.RatingValue = dto.RatingValue;
        }
        else
        {
            await _unitOfWork.ProductRating.AddAsync(new ProductRating
            {
                UserId = userId,
                ProductId = dto.ProductId,
                RatingValue = dto.RatingValue
            });
        }

        var comment = new ProductComment
        {
            ProductId = dto.ProductId,
            UserId = userId,
            CommentText = dto.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ProductComment.AddAsync(comment);
        await _unitOfWork.Complete();

        return Ok(new { Message = "Rating and comment submitted successfully." });
    }


    [HttpGet("GetAllProductComments/{productId}")]
    public async Task<IActionResult> GetAllProductComments(int productId)
    {
        // ✅ Validate Product Existence (Avoid unnecessary queries)
        var product = await _unitOfWork.Product.GetByIdAsync(productId);
        if (product == null)
            return NotFound(new { Message = "Product not found." });

        double averageRating = (product.Ratings != null && product.Ratings.Any())
            ? Math.Round(product.Ratings.Average(r => r.RatingValue), 2)
            : 0;

        int totalRatings = product.Ratings?.Count ?? 0;

        // Calculate rating breakdown percentages
        var ratingBreakdown = new Dictionary<int, int>();
        for (int i = 1; i <= 5; i++)
        {
            ratingBreakdown[i] = 0;
        }

        if (product.Ratings != null && product.Ratings.Any())
        {
            foreach (var rating in product.Ratings)
            {
                if (rating.RatingValue >= 1 && rating.RatingValue <= 5)
                {
                    ratingBreakdown[(int)rating.RatingValue]++;
                }
            }
        }

        // Calculate percentages
        var ratingPercentages = new Dictionary<string, double>();
        foreach (var pair in ratingBreakdown)
        {
            double percentage = totalRatings > 0
                ? Math.Round((double)pair.Value / totalRatings, 2)
                : 0;
            ratingPercentages[$"{pair.Key} star"] = percentage;
        }

        // ✅ Retrieve comments efficiently
        var comments = await _unitOfWork.ProductComment.GetCommentsByProductIdAsync(productId);

        // ✅ Return structured response
        return Ok(new
        {
            Message = "Comments retrieved successfully.",
            NumOfComments = comments.Count,
            Comments = comments.Select(comment => new
            {
                CommentId = comment.Id,
                UserId = comment.UserId,
                UserName = comment.User != null ? $"{comment.User.FirstName} {comment.User.LastName}" : "Unknown",
                CommentText = comment.CommentText,
                CreatedAt = comment.CreatedAt
            }),
            AverageRating = averageRating,
            RatingBreakdown = ratingPercentages
        });
    }

    [HttpGet("best-selling")]
    public async Task<IActionResult> GetBestSellingProducts([FromQuery] int top = 10)
    {
        var bestSellingProducts = await _unitOfWork.Product.GetBestSellingProductsAsync(top);
        var response = bestSellingProducts.Select(product =>
        {
            // Calculate rating breakdown percentages
            var ratingBreakdown = new Dictionary<int, int>();
            for (int i = 1; i <= 5; i++)
            {
                ratingBreakdown[i] = 0;
            }

            int totalRatings = product.Ratings?.Count() ?? 0;

            if (product.Ratings != null && product.Ratings.Any())
            {
                foreach (var rating in product.Ratings)
                {
                    if (rating.RatingValue >= 1 && rating.RatingValue <= 5)
                    {
                        ratingBreakdown[(int)rating.RatingValue]++;
                    }
                }
            }

            // Calculate percentages
            var ratingPercentages = new Dictionary<string, double>();
            foreach (var pair in ratingBreakdown)
            {
                double percentage = totalRatings > 0
                    ? Math.Round((double)pair.Value / totalRatings, 2)
                    : 0;
                ratingPercentages[$"{pair.Key} star"] = percentage;
            }

            return new Best_SeillingResponseDTO
            {
                ProductId = product.Id,
                ProductName = product.Name,
                AuthorId = product.AuthorId,
                AuthorName = product.Author != null ? product.Author.FirstName + " " + product.Author.LastName : "Unknown",
                PriceBeforeDiscount = (double)product.Price,
                PriceAfterDiscount = (double)(product.Price * (1 - product.Discount / 100)),
                HomePictureUrl = $"https://innova-hub.premiumasp.net{product.HomePicture}",
                ProductPictures = product.ProductPictures != null
                    ? product.ProductPictures.Select(pic =>
                        $"https://innova-hub.premiumasp.net{pic.PictureUrl}").ToList()
                    : new List<string>(),
                Stock = product.Stock,
                Rating = product.Ratings.Any() ? product.Ratings.Average(r => r.RatingValue) : 0,
                NumberOfRatings = totalRatings,
                RatingBreakdown = ratingPercentages
            };
        }).ToList();

        return Ok(response);
    }
}
