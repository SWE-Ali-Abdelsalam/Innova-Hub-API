using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InnoHub.Core.Models;

namespace InnoHub.Core.IRepository
{
    public interface IReport : IGenericRepository<Report>
    {
        public Task<List<Report>> GetAllReports();
    }
}
