using Moq;
using Podium.Core.Interfaces;
using Podium.Application.Interfaces;
using Podium.Application.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Podium.Tests
{
    /// <summary>
    /// Base class for unit tests providing common setup
    /// </summary>
    public abstract class TestBase
    {
        protected Mock<IUnitOfWork> MockUnitOfWork { get; }
        protected Mock<IEmailService> MockEmailService { get; }
        protected Mock<INotificationService> MockNotificationService { get; }
        protected Mock<IPermissionService> MockPermissionService { get; }
        protected Mock<IVideoStorageService> MockVideoStorageService { get; }
        protected Mock<IConfiguration> MockConfiguration { get; }

        protected TestBase()
        {
            MockUnitOfWork = new Mock<IUnitOfWork>();
            MockEmailService = new Mock<IEmailService>();
            MockNotificationService = new Mock<INotificationService>();
            MockPermissionService = new Mock<IPermissionService>();
            MockVideoStorageService = new Mock<IVideoStorageService>();
            MockConfiguration = new Mock<IConfiguration>();

            SetupDefaultMocks();
        }

        /// <summary>
        /// Override this method to set up default mock behavior in derived classes
        /// </summary>
        protected virtual void SetupDefaultMocks()
        {
            // Default setup can be added here or overridden in derived classes
        }

        protected Mock<ILogger<T>> MockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }
    }
}
