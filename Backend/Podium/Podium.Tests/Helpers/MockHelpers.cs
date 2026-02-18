using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Podium.Core.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Podium.Tests.Helpers
{
    /// <summary>
    /// Helper class for creating common mocks used in unit tests
    /// </summary>
    public static class MockHelpers
    {
        /// <summary>
        /// Creates a mock UserManager for testing Identity operations
        /// </summary>
        public static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<ApplicationUser>>().Object,
                new IUserValidator<ApplicationUser>[0],
                new IPasswordValidator<ApplicationUser>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<ApplicationUser>>>().Object);

            return mockUserManager;
        }

        /// <summary>
        /// Creates a mock RoleManager for testing role operations
        /// </summary>
        public static Mock<RoleManager<IdentityRole>> MockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            var mockRoleManager = new Mock<RoleManager<IdentityRole>>(
                store.Object,
                new IRoleValidator<IdentityRole>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<ILogger<RoleManager<IdentityRole>>>().Object);

            return mockRoleManager;
        }

        /// <summary>
        /// Creates a mock IQueryable for use with Entity Framework Include operations
        /// </summary>
        public static IQueryable<T> MockQueryable<T>(List<T> items) where T : class
        {
            return items.AsQueryable();
        }

        /// <summary>
        /// Sets up a mock repository to return a queryable collection
        /// </summary>
        public static void SetupMockQueryable<T>(Mock<Podium.Core.Interfaces.IRepository<T>> mockRepo, List<T> items) where T : class
        {
            mockRepo.Setup(r => r.GetQueryable()).Returns(items.AsQueryable());
        }
    }
}
