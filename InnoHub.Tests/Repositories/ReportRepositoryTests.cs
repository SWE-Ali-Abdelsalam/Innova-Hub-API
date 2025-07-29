using FluentAssertions;
using InnoHub.Core.Models;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InnoHub.Tests.Repositories
{
    public class ReportRepositoryTests : BaseRepositoryTest
    {
        private readonly ReportRepository _reportRepository;

        public ReportRepositoryTests()
        {
            _reportRepository = new ReportRepository(Context);
        }

        [Fact]
        public async Task GetAllReports_ShouldReturnAllReports()
        {
            // Arrange
            await SeedTestDataAsync();
            var report = new Report
            {
                Id = 1,
                ReporterId = "test-user-id",
                ReportedId = "1",
                ReportedType = ReportedEntityType.Product,
                Message = "This product is inappropriate",
                CreatedAt = DateTime.UtcNow
            };
            Context.Reports.Add(report);
            await Context.SaveChangesAsync();

            // Act
            var result = await _reportRepository.GetAllReports();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Message.Should().Be("This product is inappropriate");
        }
    }
}