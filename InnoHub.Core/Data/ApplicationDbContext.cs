using InnoHub.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Text.Json;

namespace InnoHub.Core.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ✅ CartItem Relationships
            builder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents cascade path conflicts

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade); // Deletes CartItem when Cart is deleted

            // ✅ WishlistItem Relationships
            builder.Entity<WishlistItem>()
                .HasOne(wi => wi.Wishlist)
                .WithMany(w => w.WishlistItems)
                .HasForeignKey(wi => wi.WishlistId)
                .OnDelete(DeleteBehavior.Cascade); // Deletes WishlistItem when Wishlist is deleted

            builder.Entity<WishlistItem>()
                .HasOne(wi => wi.Product)
                .WithMany(p => p.WishlistItems)
                .HasForeignKey(wi => wi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents multiple cascade paths

            // ✅ ProductRating Relationships
            builder.Entity<ProductRating>()
                .HasOne(pr => pr.Product)
                .WithMany(p => p.Ratings)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents cascade path conflicts

            // ✅ Deal → Category Relationship
            builder.Entity<Deal>()
                .HasOne(d => d.Category)
                .WithMany(c => c.Deals)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Keeps Deals even if Category is deleted

            // ✅ Define Composite Key for ProductRating
            builder.Entity<ProductRating>()
                    .HasKey(pr => new { pr.UserId, pr.ProductId });

            // ✅ Define Foreign Key Relationships
            builder.Entity<ProductRating>()
                .HasOne(pr => pr.User)
                .WithMany()
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductRating>()
                .HasOne(pr => pr.Product)
                .WithMany(p => p.Ratings)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductComment>()
                .HasOne(pc => pc.Product)
                .WithMany(p => p.Comments)  // Add Comments collection to Product Model
                .HasForeignKey(pc => pc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductComment>()
                .HasOne(pc => pc.User)
                .WithMany()
                .HasForeignKey(pc => pc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
                .HasMany(p => p.ProductPictures)
                .WithOne(pp => pp.Product)
                .HasForeignKey(pp => pp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many relationship between Product and Size
            builder.Entity<Product>()
                .HasMany(p => p.Sizes)
                .WithOne(s => s.Product)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many relationship between Product and Color
            builder.Entity<Product>()
                .HasMany(p => p.Colors)
                .WithOne(c => c.Product)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Order>()
                .HasOne(o => o.ShippingAddress)
                .WithOne(sa => sa.Order)
                .HasForeignKey<Order>(o => o.ShippingAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .HasOne(o => o.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(o => o.OrderId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascading delete

            builder.Entity<OrderItem>()
                .HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Order>()
                .HasIndex(o => o.ShippingAddressId)
                .IsUnique(false); // Allow multiple orders to use the same shipping address//


            // InvestmentMessage relationships
            builder.Entity<DealMessage>()
                .HasOne(m => m.Deal)
                .WithMany(i => i.Messages)
                .HasForeignKey(m => m.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Deal>()
                .HasOne(m => m.Product)
                .WithMany(i => i.Deals)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DealMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DealMessage>()
                .HasOne(m => m.Recipient)
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            // InvestmentProfit relationships
            builder.Entity<DealProfit>()
                .HasOne(p => p.Deal)
                .WithMany(i => i.ProfitDistributions)
                .HasForeignKey(p => p.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // InvestmentTransaction relationships
            builder.Entity<DealTransaction>()
                .HasOne(t => t.Deal)
                .WithMany()
                .HasForeignKey(t => t.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // Fix the Investment-Product relationship
            builder.Entity<AppUser>()
                .HasOne(u => u.Deal)
                .WithOne(p => p.Investor)
                .HasForeignKey<Deal>(p => p.InvestorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Deal>()
    .HasOne(d => d.DealChangeRequest)
    .WithOne(dcr => dcr.Deal)
    .HasForeignKey<DealChangeRequest>(dcr => dcr.DealId);

            builder.Entity<Deal>()
                .HasOne(u => u.DealDeleteRequest)
                .WithOne(p => p.Deal)
                .HasForeignKey<DealDeleteRequest>(p => p.DealId);

            builder.Entity<DealMessage>()
                .Property(m => m.MessageType)
                .HasDefaultValue(MessageType.General);

            builder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId);

            // إضافة العلاقات الجديدة للـ Deal
            
    //        builder.Entity<Deal>()
    //.Property(e => e.Pictures)
    //.HasConversion(
    //    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
    //    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>()
    //);


            // Fix decimal column types
            // AppUser
            builder.Entity<AppUser>()
                .Property(u => u.TotalAccountBalance)
                .HasColumnType("decimal(18,2)");

            // Cart
            builder.Entity<Cart>()
                .Property(c => c.TotalPrice)
                .HasColumnType("decimal(18,2)");

            // CartItem
            builder.Entity<CartItem>()
                .Property(ci => ci.Price)
                .HasColumnType("decimal(18,2)");

            // Deal
            builder.Entity<Deal>()
                .Property(d => d.EstimatedPrice)
                .HasColumnType("decimal(18,2)");
            builder.Entity<Deal>()
                .Property(d => d.ManufacturingCost)
                .HasColumnType("decimal(18,2)");
            builder.Entity<Deal>()
                .Property(d => d.OfferDeal)
                .HasColumnType("decimal(18,2)");
            builder.Entity<Deal>()
                .Property(d => d.OfferMoney)
                .HasColumnType("decimal(18,2)");

            // DeliveryMethod
            builder.Entity<DeliveryMethod>()
                .Property(dm => dm.Cost)
                .HasColumnType("decimal(18,2)");

            // Investment
            builder.Entity<Deal>()
                .Property(i => i.OfferDeal)
                .HasColumnType("decimal(18,2)");
            builder.Entity<Deal>()
                .Property(i => i.OfferMoney)
                .HasColumnType("decimal(18,2)");
            builder.Entity<Deal>()
                .Property(i => i.PlatformFeePercentage)
                .HasColumnType("decimal(18,2)");

            // InvestmentProfit
            builder.Entity<DealProfit>()
                .Property(ip => ip.TotalRevenue)
                .HasColumnType("decimal(18,2)");
            builder.Entity<DealProfit>()
                .Property(ip => ip.ManufacturingCost)
                .HasColumnType("decimal(18,2)");
            builder.Entity<DealProfit>()
                .Property(ip => ip.OtherCosts)
                .HasColumnType("decimal(18,2)");
            builder.Entity<DealProfit>()
                .Property(ip => ip.NetProfit)
                .HasColumnType("decimal(18,2)");
            builder.Entity<DealProfit>()
                .Property(ip => ip.InvestorShare)
                .HasColumnType("decimal(18,2)");
            builder.Entity<DealProfit>()
                .Property(ip => ip.OwnerShare)
                .HasColumnType("decimal(18,2)");
            builder.Entity<DealProfit>()
                .Property(ip => ip.PlatformFee)
                .HasColumnType("decimal(18,2)");

            // InvestmentTransaction
            builder.Entity<DealTransaction>()
                .Property(it => it.Amount)
                .HasColumnType("decimal(18,2)");

            // Order
            builder.Entity<Order>()
                .Property(o => o.Subtotal)
                .HasColumnType("decimal(18,2)");
            builder.Entity<Order>()
                .Property(o => o.ShippingCost)
                .HasColumnType("decimal(18,2)");
            builder.Entity<Order>()
                .Property(o => o.Tax)
                .HasColumnType("decimal(18,2)");
            builder.Entity<Order>()
                .Property(o => o.Total)
                .HasColumnType("decimal(18,2)");

            // OrderItem
            builder.Entity<OrderItem>()
                .Property(oi => oi.Price)
                .HasColumnType("decimal(18,2)");

            // PaymentRefundLog
            builder.Entity<PaymentRefundLog>()
                .Property(prl => prl.RefundAmount)
                .HasColumnType("decimal(18,2)");

            // Product
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");
            builder.Entity<Product>()
                .Property(p => p.Discount)
                .HasColumnType("decimal(18,2)");

            // OrderItem
            builder.Entity<OrderItem>()
                .Property(p => p.Profit)
                .HasColumnType("decimal(18,2)");

            builder.Entity<AppUser>()
                .Property(u => u.ProfileImageUrl)
                .HasDefaultValue("/ProfileImages/DefaultImage.png");

            builder.Entity<AppUser>()
                .Property(u => u.ProfileCoverUrl)
                .HasDefaultValue("/ProfileImages/DefaultCover.jpg");

            builder.Entity<AppUser>()
                .Property(u => u.PostalCode)
                .HasDefaultValue("11511");

            builder.Entity<AppUser>()
                .Property(u => u.IsStripeAccountEnabled)
                .HasDefaultValue(false);

            builder.Entity<AppUser>()
                .Property(u => u.IsIdCardVerified)
                .HasDefaultValue(false);

            builder.Entity<AppUser>()
                .Property(u => u.IsSignatureVerified)
                .HasDefaultValue(false);

            builder.Entity<AppUser>()
                .Property(u => u.Isblock)
                .HasDefaultValue(false);

            builder.Entity<Deal>()
                .Property(u => u.PlatformFeePercentage)
                .HasDefaultValue(1);

            builder.Entity<OrderItem>()
                .Property(e => e.ShipDate)
                .HasDefaultValueSql("DATEADD(MINUTE, 30, GETUTCDATE())");

            //builder.Entity<OrderDate>()
            //    .Property(e => e.Date)
            //    .HasDefaultValueSql("GETUTCDATE()");

            // إضافة القيم الافتراضية للحقول الجديدة

            builder.Entity<Deal>()
                .Property(d => d.IsChangePaymentRequired)
                .HasDefaultValue(false);

            builder.Entity<Deal>()
                .Property(d => d.IsChangePaymentProcessed)
                .HasDefaultValue(false);

            builder.Entity<Deal>()
                .Property(d => d.ContractVersion)
                .HasDefaultValue(1);

            // إضافة تكوين للحقول العشرية الجديدة
            builder.Entity<Deal>()
                .Property(d => d.ChangeAmountDifference)
                .HasColumnType("decimal(18,2)");

            // إضافة مؤشرات للأداء
            
            builder.Entity<Deal>()
                .HasIndex(d => new { d.IsChangePaymentRequired, d.IsChangePaymentProcessed })
                .HasDatabaseName("IX_Deal_ChangePaymentStatus");

            builder.Entity<Deal>()
                .HasIndex(d => d.ContractVersion)
                .HasDatabaseName("IX_Deal_ContractVersion");

            builder.Entity<Deal>()
                .HasIndex(d => d.LastContractGeneratedAt)
                .HasDatabaseName("IX_Deal_LastContractGenerated");

            // إضافة قيود على الحقول النصية لمنع تخزين النصوص الطويلة جداً
            
            builder.Entity<Deal>()
                .Property(d => d.ChangePaymentIntentId)
                .HasMaxLength(255);

            builder.Entity<Deal>()
                .Property(d => d.LastProcessedPaymentHash)
                .HasMaxLength(64); // SHA-256 hash length

            builder.Entity<Deal>()
                .Property(d => d.PreviousContractDocumentUrl)
                .HasMaxLength(500);

            // إضافة تكوين لحماية البيانات الحساسة
            builder.Entity<Deal>()
                .Property(d => d.LastProcessedPaymentHash)
                .IsRequired(false);

            // إضافة قيود الفحص للتأكد من صحة البيانات
            builder.Entity<Deal>().ToTable(tb =>
            {
                tb.HasCheckConstraint("CK_Deal_ContractVersion", "[ContractVersion] >= 1");
                tb.HasCheckConstraint("CK_Deal_ChangePayment", "([IsChangePaymentRequired] = 0 OR [ChangeRequestId] IS NOT NULL)");
            });


            // إضافة تكوين للعمليات المتزامنة (Concurrency)
            builder.Entity<Deal>()
                .Property<byte[]>("RowVersion")
                .IsRowVersion();

            // تكوين إضافي لتحسين الأداء - Temporal Tables (اختياري)
            // builder.Entity<Deal>().ToTable("Deals", b => b.IsTemporal());

            // إضافة Seed Data للحقول الجديدة (اختياري)
            builder.Entity<Deal>().HasData(
            // يمكن إضافة بيانات تجريبية هنا إذا لزم الأمر
            );

            // تكوين العلاقات المعقدة
            
            // تكوين JSON للحقول المعقدة (إذا كنت تستخدم EF Core 7+)
            /*
            builder.Entity<DealChangeRequest>()
                .OwnsOne(e => e.OriginalValuesJson, builder =>
                {
                    builder.ToJson();
                    builder.Property(e => e.InvestmentAmount);
                    builder.Property(e => e.EquityPercentage);
                    // ... other properties
                });
            */
        }

        // DbSet properties
        public DbSet<Report> Reports { get; set; }
        public DbSet<PaymentRefundLog>  PaymentRefundLogs { get; set; }
        public DbSet<OrderReturnRequest> OrderReturnRequests { get; set; }
        public DbSet<PaymentFailureLog> PaymentFailureLogs { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems  { get; set; }
        public DbSet<DeliveryMethod> DeliveryMethods    { get; set; }
        public DbSet<ShippingAddress> ShippingAddresses { get; set; }
        public DbSet<ProductPicture> ProductPictures { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }
        public DbSet<ProductColor> ProductColors { get; set; }
        public DbSet<ProductComment> ProductComments { get; set; }

        public DbSet<Deal> Deals { get; set; }
        public DbSet<DealChangeRequest> DealChangeRequests { get; set; }
        public DbSet<DealDeleteRequest> DealDeleteRequests { get; set; }
        public DbSet<ProductRating> ProductRatings { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }

        public DbSet<DealMessage> DealMessages { get; set; }
        public DbSet<DealProfit> DealProfits { get; set; }
        public DbSet<DealTransaction> DealTransactions { get; set; }
        public DbSet<NotificationMessage> Notifications { get; set; }

        //================================================================

        //public DbSet<Owner> Owners { get; set; }
        //public DbSet<OrderDate> OrderDates { get; set; }
    }
}
