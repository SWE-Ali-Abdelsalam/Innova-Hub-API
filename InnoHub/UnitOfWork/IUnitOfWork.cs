using Ecommerce_platforms.Repository.Auth;
using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Service.FileService;
using Microsoft.EntityFrameworkCore.Storage;

public interface IUnitOfWork : IDisposable
{
    IReport Report { get; }
    IOrderReturnRequest OrderReturnRequest { get; }
    IPaymentRefundLog PaymentRefundLog { get; }
    IPaymentFailureLog PaymentFailureLog { get; }
    IOrder Order { get; }
    IOrderItem OrderItem { get; }
    IDeliveryMethod DeliveryMethod { get; }
    IShippingAddress shippingAddress { get; }
    IFileService FileService { get; }
    IAuth Auth { get; }
    ICart Cart { get; }
    IProduct Product { get; }
    ICategory Category { get; }
    IProductRating ProductRating { get; }
    IWishlist Wishlist { get; }
    IDeal Deal { get; }
    IDealChangeRequest DealChangeRequest { get; }
    IDealDeleteRequest DealDeleteRequest { get; }
    IWishlistItem WishlistItem { get; }
    IProductComment ProductComment { get; }
    IDealMessage InvestmentMessage { get; }
    IDealProfit InvestmentProfit { get; }
    IDealTransaction InvestmentTransaction { get; }
    INotification Notification { get; }
    IAppUser AppUser { get; }
    //IOwner Owner { get; }
    //IOrderDate OrderDate { get; }
    ApplicationDbContext DbContext { get; }
    // 🔹 Add missing method:
    public Task<IDbContextTransaction> BeginTransactionAsync();
     
    Task<int> Complete();
}
