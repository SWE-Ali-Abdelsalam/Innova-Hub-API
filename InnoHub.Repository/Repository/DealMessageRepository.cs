using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace InnoHub.Repository.Repository
{
    public class DealMessageRepository : GenericRepository<DealMessage>, IDealMessage
    {
        public DealMessageRepository(ApplicationDbContext context) : base(context) { }

        //public async Task<IEnumerable<DealMessage>> GetMessagesByDealId(int dealId)
        //{
        //    return await _context.DealMessages
        //        .Include(m => m.Sender)
        //        .Include(m => m.Recipient)
        //        .Where(m => m.DealId == dealId)
        //        .OrderBy(m => m.CreatedAt)
        //        .ToListAsync();
        //}

        public async Task<IEnumerable<DealMessage>> GetMessages(string recipientId, int dealId, bool onlyUnread = false)
        {
            var query = _context.DealMessages
                .Include(m => m.Sender)
                .Include(m => m.Deal)
                .Where(m => m.RecipientId == recipientId && m.DealId == dealId);

            if (onlyUnread)
                query = query.Where(m => !m.IsRead);

            return await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
        }

        public async Task<IEnumerable<DealMessage>> GetMessagesByRecipientId(string recipientId, bool onlyUnread = false)
        {
            var query = _context.DealMessages
                .Include(m => m.Sender)
                .Include(m => m.Deal)
                .Where(m => m.RecipientId == recipientId);

            if (onlyUnread)
                query = query.Where(m => !m.IsRead);

            return await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
        }
    }
}
