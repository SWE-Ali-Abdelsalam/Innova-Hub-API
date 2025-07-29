using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IDealMessage : IGenericRepository<DealMessage>
    {
        //Task<IEnumerable<DealMessage>> GetMessagesByDealId(int dealId);
        Task<IEnumerable<DealMessage>> GetMessages(string recipientId, int dealId, bool onlyUnread = false);
        Task<IEnumerable<DealMessage>> GetMessagesByRecipientId(string recipientId, bool onlyUnread = false);
    }
}
