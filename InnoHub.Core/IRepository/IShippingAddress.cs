using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IShippingAddress : IGenericRepository<ShippingAddress>
    {
        public Task<ShippingAddress> GetShippingAddressByUserId(string userId);
        public Task<bool> UserHasShippingAddress(string userId);
    }
}
