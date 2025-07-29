using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Tests.Helpers
{
    public static class TestDataHelper
    {
        public static AppUser CreateTestUser(string id = "test-user-id", string email = "test@example.com")
        {
            return new AppUser
            {
                Id = id,
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
                City = "Test City",
                Country = "Test Country",
                PhoneNumber = "1234567890",
                EmailConfirmed = true,
                TotalAccountBalance = 1000m,
                RegisteredAt = DateTime.UtcNow
            };
        }

        public static Product CreateTestProduct(int id = 1, string authorId = "test-user-id")
        {
            return new Product
            {
                Id = id,
                Name = "Test Product",
                Description = "Test Description",
                Price = 100m,
                Stock = 10,
                Discount = 0,
                AuthorId = authorId,
                CategoryId = 1,
                HomePicture = "/test/image.jpg",
                Dimensions = "10x10x10",
                Weight = 1.5
            };
        }

        public static Deal CreateTestDeal(int id = 1, string authorId = "test-user-id", string investorId = "test-investor-id")
        {
            return new Deal
            {
                Id = id,
                AuthorId = authorId,
                InvestorId = investorId,
                BusinessName = "Test Business",
                Description = "Test Deal Description",
                OfferMoney = 10000m,
                OfferDeal = 20m,
                CategoryId = 1,
                ManufacturingCost = 50m,
                EstimatedPrice = 100m,
                Status = DealStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                DurationInMonths = 12
            };
        }

        public static Cart CreateTestCart(string userId = "test-user-id")
        {
            return new Cart
            {
                Id = 1,
                UserId = userId,
                TotalPrice = 200m,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = 1,
                        ProductId = 1,
                        Quantity = 2,
                        Price = 100m
                    }
                }
            };
        }

        public static Order CreateTestOrder(string userId = "test-user-id")
        {
            return new Order
            {
                Id = 1,
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                OrderStatus = OrderStatus.Pending,
                Subtotal = 200m,
                Tax = 4m,
                ShippingCost = 10m,
                Total = 214m,
                DeliveryMethodId = 1,
                ShippingAddressId = 1,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = 1,
                        ProductId = 1,
                        Quantity = 2,
                        Price = 100m
                    }
                }
            };
        }

        public static Category CreateTestCategory(int id = 1)
        {
            return new Category
            {
                Id = id,
                Name = "Test Category",
                Description = "Test Description",
                ImageUrl = "/test/image.jpg",
                IsPopular = true
            };
        }

        public static Wishlist CreateTestWishlist(string userId = "test-user-id")
        {
            return new Wishlist
            {
                Id = 1,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                WishlistItems = new List<WishlistItem>
                {
                    new WishlistItem
                    {
                        Id = 1,
                        WishlistId = 1,
                        ProductId = 1
                    }
                }
            };
        }

        public static ShippingAddress CreateTestShippingAddress(string userId = "test-user-id")
        {
            return new ShippingAddress
            {
                Id = 1,
                UserId = userId,
                FirstName = "Test",
                LastName = "User",
                StreetAddress = "123 Test St",
                City = "Test City",
                ZipCode = "12345",
                Email = "test@example.com",
                Phone = "1234567890"
            };
        }
    }
}
