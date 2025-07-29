using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Repository.Repository
{
    public class ShippingAddressRepository : GenericRepository<ShippingAddress>, IShippingAddress
    {
        public ShippingAddressRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<bool> UserHasShippingAddress(string userId)
        {
            var existingAddress = await _context.ShippingAddresses
                .AnyAsync(x => x.UserId == userId);

            return existingAddress; // Returns true if the user already has an address
        }

        public async Task<ShippingAddress> GetShippingAddressByUserId(string userId)
        {
            return await _context.ShippingAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

    }
}
