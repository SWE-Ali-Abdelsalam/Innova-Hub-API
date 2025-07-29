using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace InnoHub.Repository.Repository
{
    public class ReportRepository : GenericRepository<Report>, IReport
    {
        public ReportRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Report>> GetAllReports()
        {
            var reports = await _context.Reports.Include(r => r.Reporter)
                                                  .ThenInclude(r => r.Products)
                                                  .ToListAsync(); // Using ToListAsync instead of ToList
            return reports; // Directly return the list
        }

    }
}
