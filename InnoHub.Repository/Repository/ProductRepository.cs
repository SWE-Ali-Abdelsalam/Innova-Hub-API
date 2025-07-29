using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InnoHub.Repository.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;

public class ProductRepository : GenericRepository<Product>, IProduct
{
    private readonly ApplicationDbContext _context;
    
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }
    
    // ✅ Get Product Count

    // ✅ Get All Products by Category ID
    public async Task<IList<Product>> GetAllProductsByCategoryId(int categoryId)
    {
        return await _context.Products
            .Where(p => p.CategoryId == categoryId)
            .Include(p=>p.ProductPictures)
            .Include(p => p.Colors)
            .Include(p=>p.Sizes)
            .ToListAsync();
    }

    // ✅ Get All Products (With Author Details)
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products
            .Include(p => p.Author)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetAllWithCategoriesAsync()
    {
        return await _context.Products
            .Include(p => p.Author)
            .Include(p => p.Category)
            .ToListAsync();
    }

    public async Task<Product> GetByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Ratings)
            .Include(p => p.Author)
            .Include(p => p.ProductPictures)  // Assuming this is a list of strings (URLs)
            .Include(p => p.Category)
                .Include(p=>p.Colors)
                .Include(p=>p.Sizes)
                .Include(p => p.Comments)
                .ThenInclude(p=>p.User)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
    // ✅ Get Products by List of IDs
    public async Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> productIds)
    {
        if (productIds == null || !productIds.Any())
            return new List<Product>();

        return await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .Include(p => p.Author)
            .Include(p => p.Ratings)
            .Include(p => p.ProductPictures)
            .ToListAsync();
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        var existingProduct = await _context.Products
            .Include(p => p.ProductPictures)
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        if (existingProduct == null)
            return false;

        _context.Entry(existingProduct).CurrentValues.SetValues(product);

        return true; // Indicate success (SaveChangesAsync will be called outside)
    }
    public void DeleteFile(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;

        var fullPath = Path.Combine("wwwroot", relativePath.TrimStart('/'));
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);
    }
    public async Task RemoveAllPicturesByProductIdAsync(int productId)
    {
        // ✅ Retrieve all product pictures associated with the given product ID
        var productPictures = await _context.ProductPictures
            .Where(pp => pp.ProductId == productId)
            .ToListAsync();

        if (productPictures.Any())
        {
            // ✅ Delete image files from the file system
            foreach (var picture in productPictures)
            {
                // Assuming the PictureUrl contains the file path of the image
                DeleteFile(picture.PictureUrl); // You can implement DeleteFile to handle file deletion
            }

            // ✅ Remove the product pictures from the database
            _context.ProductPictures.RemoveRange(productPictures);

            // ✅ Commit changes to the database
            await _context.SaveChangesAsync();
        }
    }

    public IQueryable<Product> GetQueryable()
    {
        return _context.Products
            .Include(p => p.Author) // Include related data
            .Include(p => p.ProductPictures)
            .Include(p => p.Category)
            .AsQueryable(); // Ensure it's queryable
    }

    public async Task<List<Product>> GetBestSellingProductsAsync(int top)
    {
        return await _context.OrderItems
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalSold = g.Sum(oi => oi.Quantity)
            })
            .OrderByDescending(g => g.TotalSold)
            .Take(top)
            .Join(_context.Products.Include(p => p.Author)
                                   .Include(p => p.ProductPictures)
                                   .Include(p => p.Ratings),
                  sale => sale.ProductId,
                  product => product.Id,
                  (sale, product) => product)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductLinkedDeals(string ownerId)
    {
        return await _context.Products
            .Include(p => p.Deals)
            .ThenInclude(i => i.Investor)
            .Where(p => p.AuthorId == ownerId && p.Deals != null)
            .ToListAsync();
    }
}
