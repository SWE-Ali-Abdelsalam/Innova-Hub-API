using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InnoHub.Core.Data
{
    public class DeliveryMethodDataSeeding
    {
        public static async Task SeedDeliveryMethodsAsync(ApplicationDbContext context)
        {
            if (!await context.DeliveryMethods.AnyAsync())
            {
                var deliveryMethods = new List<DeliveryMethod>
                {
                    new DeliveryMethod { ShortName = "UPS1", Description = "Fastest delivery time", DeliveryTime = "1-2 Days", Cost = 50 },
                    new DeliveryMethod { ShortName = "UPS2", Description = "Get it within 5 days", DeliveryTime = "2-5 Days", Cost = 30 },
                    new DeliveryMethod { ShortName = "UPS3", Description = "Slower but cheap", DeliveryTime = "5-10 Days", Cost = 20 },
                    new DeliveryMethod { ShortName = "FREE", Description = "Free! You get what you pay for", DeliveryTime = "1-2 Weeks", Cost = 0 }
                };

                await context.DeliveryMethods.AddRangeAsync(deliveryMethods);
                await context.SaveChangesAsync();
            }
        }
    }
}
