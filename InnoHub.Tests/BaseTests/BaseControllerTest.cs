using AutoMapper;
using Castle.Core.Configuration;
using InnoHub.Core.Models;
using InnoHub.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Tests.BaseTests
{
    public abstract class BaseControllerTest
    {
        protected Mock<IUnitOfWork> MockUnitOfWork { get; }
        protected Mock<UserManager<AppUser>> MockUserManager { get; }
        protected Mock<RoleManager<IdentityRole>> MockRoleManager { get; }
        protected Mock<IConfiguration> MockConfiguration { get; }
        protected IMapper Mapper { get; }
        protected Mock<ILogger<T>> GetMockLogger<T>() => new Mock<ILogger<T>>();

        public BaseControllerTest()
        {
            MockUnitOfWork = new Mock<IUnitOfWork>();
            MockUserManager = GetMockUserManager();
            MockRoleManager = GetMockRoleManager();
            MockConfiguration = new Mock<IConfiguration>();

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            Mapper = config.CreateMapper();
        }

        protected Mock<UserManager<AppUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        protected Mock<RoleManager<IdentityRole>> GetMockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(store.Object, null, null, null, null);
        }

        protected ControllerContext GetControllerContext(string userId = "test-user-id")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Role, "Customer")
            }, "Test"));

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        protected string GetValidAuthorizationHeader() => "Bearer valid-jwt-token";
    }
}
