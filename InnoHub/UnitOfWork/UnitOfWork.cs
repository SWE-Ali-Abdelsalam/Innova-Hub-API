using Ecommerce_platforms.Repository.Auth;
using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using InnoHub.Repository.Repository;
using InnoHub.Service.FileService;
using Microsoft.EntityFrameworkCore.Storage;

namespace InnoHub.UnitOfWork
{
    public class UnitofWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;
        private readonly Lazy<IDeal> _deal;
        private readonly Lazy<IDealMessage> _investmentMessage;
        private readonly Lazy<IDealProfit> _investmentProfit;
        private readonly Lazy<IDealTransaction> _investmentTransaction;
        private readonly Lazy<IOrderItem> _orderItem;
        private readonly Lazy<IDealChangeRequest> _dealChangeRequest;
        private readonly Lazy<IDealDeleteRequest> _dealDeleteRequest;
        private readonly Lazy<INotification> _notification;

        public UnitofWork(
            ApplicationDbContext context,
            ICart cart,
            IProduct product,
            ICategory category,
            IProductRating productRating,
            IWishlist wishlist,
            IDeal deal,
            IWishlistItem wishlistItem,
            IProductComment productComment,
            IAuth auth,
            IFileService fileService,
            IShippingAddress shippingAddress,
            IDeliveryMethod deliveryMethod,
            IOrder order,
            IPaymentFailureLog paymentFailureLog,
            IOrderReturnRequest orderReturnRequest,
            IAppUser appUser,
            IReport report,
            IPaymentRefundLog paymentRefundLog)
        {
            _context = context;

            // Direct assignments from constructor parameters
            Order = order;
            Cart = cart;
            Product = product;
            Category = category;
            ProductRating = productRating;
            Wishlist = wishlist;
            WishlistItem = wishlistItem;
            ProductComment = productComment;
            Auth = auth;
            FileService = fileService;
            this.shippingAddress = shippingAddress;
            DeliveryMethod = deliveryMethod;
            PaymentFailureLog = paymentFailureLog;
            OrderReturnRequest = orderReturnRequest;
            AppUser = appUser;
            PaymentRefundLog = paymentRefundLog;
            Report = report;
            // Lazy initialization for repositories
            _deal = new Lazy<IDeal>(() => new DealRepository(context));
            _investmentMessage = new Lazy<IDealMessage>(() => new DealMessageRepository(context));
            _investmentProfit = new Lazy<IDealProfit>(() => new DealProfitRepository(context));
            _investmentTransaction = new Lazy<IDealTransaction>(() => new DealTransactionRepository(context));
            _orderItem = new Lazy<IOrderItem>(() => new OrderItemRepository(context));
            _dealChangeRequest = new Lazy<IDealChangeRequest>(() => new DealChangeRequestRepository(context));
            _dealDeleteRequest = new Lazy<IDealDeleteRequest>(() => new DealDeleteRequestRepository(context));
            _notification = new Lazy<INotification>(() => new NotificationRepository(context));
        }
        
        public ApplicationDbContext DbContext => _context;
        public IPaymentRefundLog PaymentRefundLog { get; }
        public IOrderReturnRequest OrderReturnRequest { get; }
        public IPaymentFailureLog PaymentFailureLog { get; }
        public IOrder Order { get; }
        public IDeliveryMethod DeliveryMethod { get; }
        public IWishlist Wishlist { get; }
        public IProductRating ProductRating { get; }
        public ICategory Category { get; }
        public ICart Cart { get; }
        public IDeal Deal => _deal.Value;
        public IProduct Product { get; }
        public IWishlistItem WishlistItem { get; }
        public IProductComment ProductComment { get; }
        public IDealMessage InvestmentMessage => _investmentMessage.Value;
        public IDealProfit InvestmentProfit => _investmentProfit.Value;
        public IDealTransaction InvestmentTransaction => _investmentTransaction.Value;
        public INotification Notification => _notification.Value;
        public IOrderItem OrderItem => _orderItem.Value;
        public IDealChangeRequest DealChangeRequest => _dealChangeRequest.Value;
        public IDealDeleteRequest DealDeleteRequest => _dealDeleteRequest.Value;
        public IAuth Auth { get; }
        public IFileService FileService { get; }
        public IAppUser AppUser { get; }
        public IShippingAddress shippingAddress { get; }

        public IReport Report { get; }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
            return _transaction;
        }

        public async Task<int> Complete()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
