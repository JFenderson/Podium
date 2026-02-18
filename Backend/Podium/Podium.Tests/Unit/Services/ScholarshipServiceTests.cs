using Xunit;
using Moq;
using FluentAssertions;
using Podium.Application.Services;
using Podium.Application.DTOs.ScholarshipOffer;
using Podium.Application.DTOs.Offer;
using Podium.Core.Entities;
using Podium.Core.Constants;
using Podium.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MockQueryable.Moq;

namespace Podium.Tests.Unit.Services
{
    public class ScholarshipServiceTests : TestBase
    {
        private readonly ScholarshipService _service;
        private readonly Mock<Podium.Core.Interfaces.IRepository<ScholarshipOffer>> _mockScholarshipRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<BandBudget>> _mockBandBudgetRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<Band>> _mockBandRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<Student>> _mockStudentRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<AuditLog>> _mockAuditLogRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<StudentGuardian>> _mockStudentGuardianRepo;

        public ScholarshipServiceTests()
        {
            _mockScholarshipRepo = new Mock<Podium.Core.Interfaces.IRepository<ScholarshipOffer>>();
            _mockBandBudgetRepo = new Mock<Podium.Core.Interfaces.IRepository<BandBudget>>();
            _mockBandRepo = new Mock<Podium.Core.Interfaces.IRepository<Band>>();
            _mockStudentRepo = new Mock<Podium.Core.Interfaces.IRepository<Student>>();
            _mockAuditLogRepo = new Mock<Podium.Core.Interfaces.IRepository<AuditLog>>();
            _mockStudentGuardianRepo = new Mock<Podium.Core.Interfaces.IRepository<StudentGuardian>>();

            MockUnitOfWork.Setup(u => u.ScholarshipOffers).Returns(_mockScholarshipRepo.Object);
            MockUnitOfWork.Setup(u => u.BandBudgets).Returns(_mockBandBudgetRepo.Object);
            MockUnitOfWork.Setup(u => u.Bands).Returns(_mockBandRepo.Object);
            MockUnitOfWork.Setup(u => u.Students).Returns(_mockStudentRepo.Object);
            MockUnitOfWork.Setup(u => u.AuditLogs).Returns(_mockAuditLogRepo.Object);
            MockUnitOfWork.Setup(u => u.StudentGuardians).Returns(_mockStudentGuardianRepo.Object);

            _service = new ScholarshipService(
                MockUnitOfWork.Object,
                MockNotificationService.Object,
                MockLogger<ScholarshipService>().Object
            );
        }

        #region CreateOfferAsync Tests

        [Fact]
        public async Task CreateOfferAsync_WithValidData_CreatesOfferSuccessfully()
        {
            // Arrange
            var dto = new CreateScholarshipOfferDto
            {
                StudentId = 1,
                BandId = 1,
                Amount = 5000,
                Description = "Test scholarship",
                OfferType = "Partial",
                RequiresGuardianApproval = false
            };

            var budget = TestDataBuilder.CreateTestBandBudget(bandId: 1, remainingAmount: 80000);
            var band = TestDataBuilder.CreateTestBand(id: 1);
            var student = TestDataBuilder.CreateTestStudent(id: 1);

            var budgets = new List<BandBudget> { budget }.AsQueryable().BuildMock();
            _mockBandBudgetRepo.Setup(r => r.GetQueryable()).Returns(budgets);
            _mockBandRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(band);
            _mockStudentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
            _mockScholarshipRepo.Setup(r => r.AddAsync(It.IsAny<ScholarshipOffer>())).Returns(Task.CompletedTask);
            _mockAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.CreateOfferAsync(dto, "director-id", isDirector: true);

            // Assert
            result.Should().NotBeNull();
            result.Amount.Should().Be(5000);
            result.Status.Should().Be(ScholarshipStatus.Sent);
            _mockScholarshipRepo.Verify(r => r.AddAsync(It.IsAny<ScholarshipOffer>()), Times.Once);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateOfferAsync_WithInsufficientBudget_ThrowsInvalidOperationException()
        {
            // Arrange
            var dto = new CreateScholarshipOfferDto
            {
                StudentId = 1,
                BandId = 1,
                Amount = 100000, // More than remaining budget
                Description = "Test scholarship",
                OfferType = "Full",
                RequiresGuardianApproval = false
            };

            var budget = TestDataBuilder.CreateTestBandBudget(bandId: 1, remainingAmount: 1000);
            var budgets = new List<BandBudget> { budget }.AsQueryable().BuildMock();
            _mockBandBudgetRepo.Setup(r => r.GetQueryable()).Returns(budgets);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateOfferAsync(dto, "director-id", isDirector: true)
            );
        }

        [Fact]
        public async Task CreateOfferAsync_WithNoBudget_ThrowsInvalidOperationException()
        {
            // Arrange
            var dto = new CreateScholarshipOfferDto
            {
                StudentId = 1,
                BandId = 1,
                Amount = 5000,
                Description = "Test scholarship",
                OfferType = "Partial",
                RequiresGuardianApproval = false
            };

            var budgets = new List<BandBudget>().AsQueryable().BuildMock();
            _mockBandBudgetRepo.Setup(r => r.GetQueryable()).Returns(budgets);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateOfferAsync(dto, "director-id", isDirector: true)
            );
        }

        [Fact]
        public async Task CreateOfferAsync_AsRecruiter_SetsPendingApprovalStatus()
        {
            // Arrange
            var dto = new CreateScholarshipOfferDto
            {
                StudentId = 1,
                BandId = 1,
                Amount = 5000,
                Description = "Test scholarship",
                OfferType = "Partial",
                RequiresGuardianApproval = false
            };

            var budget = TestDataBuilder.CreateTestBandBudget(bandId: 1, remainingAmount: 80000);
            var band = TestDataBuilder.CreateTestBand(id: 1);
            var student = TestDataBuilder.CreateTestStudent(id: 1);

            var budgets = new List<BandBudget> { budget }.AsQueryable().BuildMock();
            _mockBandBudgetRepo.Setup(r => r.GetQueryable()).Returns(budgets);
            _mockBandRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(band);
            _mockStudentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
            _mockScholarshipRepo.Setup(r => r.AddAsync(It.IsAny<ScholarshipOffer>())).Returns(Task.CompletedTask);
            _mockAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.CreateOfferAsync(dto, "recruiter-id", isDirector: false);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(ScholarshipStatus.PendingApproval);
        }

        [Fact]
        public async Task CreateOfferAsync_WithBandNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dto = new CreateScholarshipOfferDto
            {
                StudentId = 1,
                BandId = 999, // Non-existent band
                Amount = 5000,
                Description = "Test scholarship",
                OfferType = "Partial",
                RequiresGuardianApproval = false
            };

            var budget = TestDataBuilder.CreateTestBandBudget(bandId: 999, remainingAmount: 80000);
            var budgets = new List<BandBudget> { budget }.AsQueryable().BuildMock();
            _mockBandBudgetRepo.Setup(r => r.GetQueryable()).Returns(budgets);
            _mockBandRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Band?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CreateOfferAsync(dto, "director-id", isDirector: true)
            );
        }

        #endregion

        #region ApproveOfferAsync Tests

        [Fact]
        public async Task ApproveOfferAsync_WithValidOffer_ApprovesSuccessfully()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                status: ScholarshipStatus.PendingApproval
            );
            var student = TestDataBuilder.CreateTestStudent(id: 1);

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);
            _mockStudentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _service.ApproveOfferAsync(1, "director-id");

            // Assert
            offer.Status.Should().Be(ScholarshipStatus.Sent);
            offer.ApprovedByUserId.Should().Be("director-id");
            offer.ApprovedAt.Should().NotBe(default(DateTime));
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ApproveOfferAsync_WithNonExistentOffer_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ScholarshipOffer?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.ApproveOfferAsync(999, "director-id")
            );
        }

        [Fact]
        public async Task ApproveOfferAsync_WithInvalidStatus_ThrowsInvalidOperationException()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                status: ScholarshipStatus.Accepted
            );

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApproveOfferAsync(1, "director-id")
            );
        }

        #endregion

        #region RespondToOfferAsync Tests

        [Fact]
        public async Task RespondToOfferAsync_StudentAccepts_UpdatesStatusCorrectly()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                status: ScholarshipStatus.Sent,
                requiresGuardianApproval: false
            );
            offer.ExpirationDate = DateTime.UtcNow.AddDays(10);

            var dto = new RespondToScholarshipOfferDto
            {
                Accept = true,
                Notes = "Excited to join!"
            };

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);
            _mockAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var studentGuardians = new List<StudentGuardian>().AsQueryable().BuildMock();
            _mockStudentGuardianRepo.Setup(r => r.GetQueryable()).Returns(studentGuardians);

            // Act
            await _service.RespondToOfferAsync(1, dto, "student-id", isGuardian: false);

            // Assert
            offer.Status.Should().Be(ScholarshipStatus.Accepted);
            offer.ResponseNotes.Should().Be("Excited to join!");
            offer.RespondedByUserId.Should().Be("student-id");
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RespondToOfferAsync_StudentAcceptsWithGuardianApprovalRequired_SetsPendingGuardianSignature()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                status: ScholarshipStatus.Sent,
                requiresGuardianApproval: true
            );
            offer.ExpirationDate = DateTime.UtcNow.AddDays(10);

            var dto = new RespondToScholarshipOfferDto
            {
                Accept = true,
                Notes = "Looking forward to it!"
            };

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);
            _mockAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var studentGuardians = new List<StudentGuardian>().AsQueryable().BuildMock();
            _mockStudentGuardianRepo.Setup(r => r.GetQueryable()).Returns(studentGuardians);

            // Act
            await _service.RespondToOfferAsync(1, dto, "student-id", isGuardian: false);

            // Assert
            offer.Status.Should().Be(ScholarshipStatus.PendingGuardianSignature);
        }

        [Fact]
        public async Task RespondToOfferAsync_StudentDeclines_RefundsBudget()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                bandId: 1,
                amount: 5000,
                status: ScholarshipStatus.Sent
            );
            offer.CreatedAt = new DateTime(2025, 1, 1);
            offer.ExpirationDate = DateTime.UtcNow.AddDays(10);

            var dto = new RespondToScholarshipOfferDto
            {
                Accept = false,
                Notes = "Chose another program"
            };

            var budget = TestDataBuilder.CreateTestBandBudget(
                bandId: 1,
                fiscalYear: DateTime.UtcNow.Year,
                allocatedAmount: 20000,
                remainingAmount: 80000
            );

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);
            _mockAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
            
            var budgets = new List<BandBudget> { budget }.AsQueryable().BuildMock();
            _mockBandBudgetRepo.Setup(r => r.GetQueryable()).Returns(budgets);
            
            var studentGuardians = new List<StudentGuardian>().AsQueryable().BuildMock();
            _mockStudentGuardianRepo.Setup(r => r.GetQueryable()).Returns(studentGuardians);

            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _service.RespondToOfferAsync(1, dto, "student-id", isGuardian: false);

            // Assert
            offer.Status.Should().Be(ScholarshipStatus.Declined);
            budget.AllocatedAmount.Should().Be(15000); // 20000 - 5000
            budget.RemainingAmount.Should().Be(85000); // 80000 + 5000
        }

        [Fact]
        public async Task RespondToOfferAsync_WithExpiredOffer_ThrowsInvalidOperationException()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                status: ScholarshipStatus.Sent
            );
            offer.ExpirationDate = DateTime.UtcNow.AddDays(-5); // Expired

            var dto = new RespondToScholarshipOfferDto
            {
                Accept = true,
                Notes = "Want to accept"
            };

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RespondToOfferAsync(1, dto, "student-id", isGuardian: false)
            );

            offer.Status.Should().Be(ScholarshipStatus.Expired);
        }

        [Fact]
        public async Task RespondToOfferAsync_WithInvalidStatus_ThrowsInvalidOperationException()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                status: ScholarshipStatus.Accepted
            );

            var dto = new RespondToScholarshipOfferDto
            {
                Accept = false,
                Notes = "Changed my mind"
            };

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RespondToOfferAsync(1, dto, "student-id", isGuardian: false)
            );
        }

        #endregion

        #region RescindOfferAsync Tests

        [Fact]
        public async Task RescindOfferAsync_WithValidOffer_RescindsSuccessfully()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                bandId: 1,
                amount: 5000,
                status: ScholarshipStatus.Sent
            );
            offer.CreatedAt = new DateTime(2025, 1, 1);

            var dto = new RescindScholarshipRequest
            {
                Reason = "Student ineligible"
            };

            var budget = TestDataBuilder.CreateTestBandBudget(
                bandId: 1,
                fiscalYear: DateTime.UtcNow.Year,
                allocatedAmount: 20000,
                remainingAmount: 80000
            );

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);
            
            var budgets = new List<BandBudget> { budget }.AsQueryable().BuildMock();
            _mockBandBudgetRepo.Setup(r => r.GetQueryable()).Returns(budgets);
            
            _mockAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _service.RescindOfferAsync(1, dto, "director-id");

            // Assert
            offer.Status.Should().Be(ScholarshipStatus.Rescinded);
            offer.RescindReason.Should().Be("Student ineligible");
            offer.RescindedByUserId.Should().Be("director-id");
            budget.AllocatedAmount.Should().Be(15000); // Refunded
            budget.RemainingAmount.Should().Be(85000);
        }

        [Fact]
        public async Task RescindOfferAsync_WithAlreadyAccepted_ThrowsInvalidOperationException()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                status: ScholarshipStatus.Accepted
            );

            var dto = new RescindScholarshipRequest
            {
                Reason = "Changed mind"
            };

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RescindOfferAsync(1, dto, "director-id")
            );
        }

        #endregion

        #region GetBudgetStatsAsync Tests

        [Fact]
        public async Task GetBudgetStatsAsync_WithValidBudget_ReturnsCorrectStats()
        {
            // Arrange
            var budget = TestDataBuilder.CreateTestBandBudget(
                bandId: 1,
                fiscalYear: DateTime.UtcNow.Year,
                totalBudget: 100000,
                allocatedAmount: 30000,
                remainingAmount: 70000
            );

            var offers = new List<ScholarshipOffer>
            {
                TestDataBuilder.CreateTestScholarshipOffer(bandId: 1, amount: 10000, status: ScholarshipStatus.Accepted),
                TestDataBuilder.CreateTestScholarshipOffer(bandId: 1, amount: 5000, status: ScholarshipStatus.Accepted),
                TestDataBuilder.CreateTestScholarshipOffer(bandId: 1, amount: 10000, status: ScholarshipStatus.Sent),
                TestDataBuilder.CreateTestScholarshipOffer(bandId: 1, amount: 5000, status: ScholarshipStatus.PendingApproval)
            };
            offers.ForEach(o => o.CreatedAt = DateTime.UtcNow);

            var budgets = new List<BandBudget> { budget }.AsQueryable().BuildMock();
            _mockBandBudgetRepo.Setup(r => r.GetQueryable()).Returns(budgets);
            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers.AsQueryable().BuildMock());

            // Act
            var result = await _service.GetBudgetStatsAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.TotalBudget.Should().Be(100000);
            result.AvailableAmount.Should().Be(70000);
            result.CommittedAmount.Should().Be(15000); // 10000 + 5000 Accepted
            result.PendingAmount.Should().Be(15000); // 10000 Sent + 5000 PendingApproval
        }

        [Fact]
        public async Task GetBudgetStatsAsync_WithNoBudget_ReturnsZeros()
        {
            // Arrange
            var budgets = new List<BandBudget>().AsQueryable().BuildMock();
            _mockBandBudgetRepo.Setup(r => r.GetQueryable()).Returns(budgets);

            // Act
            var result = await _service.GetBudgetStatsAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.TotalBudget.Should().Be(0);
            result.AvailableAmount.Should().Be(0);
            result.AllocatedAmount.Should().Be(0);
            result.PendingAmount.Should().Be(0);
        }

        #endregion

        #region GetOfferByIdAsync Tests

        [Fact]
        public async Task GetOfferByIdAsync_WithValidId_ReturnsOffer()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(id: 1);
            offer.Student = TestDataBuilder.CreateTestStudent(id: 1);
            offer.Band = TestDataBuilder.CreateTestBand(id: 1);

            var offers = new List<ScholarshipOffer> { offer }.AsQueryable().BuildMock();
            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            // Act
            var result = await _service.GetOfferByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.OfferId.Should().Be(1);
            result.StudentName.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetOfferByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var offers = new List<ScholarshipOffer>().AsQueryable().BuildMock();
            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            // Act
            var result = await _service.GetOfferByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GuardianFinalizeOfferAsync Tests

        [Fact]
        public async Task GuardianFinalizeOfferAsync_AcceptingOffer_UpdatesToAccepted()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                status: ScholarshipStatus.PendingGuardianSignature
            );
            offer.ExpirationDate = DateTime.UtcNow.AddDays(10);

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _service.GuardianFinalizeOfferAsync(1, "guardian-id", accept: true);

            // Assert
            offer.Status.Should().Be(ScholarshipStatus.Accepted);
            offer.RespondedByGuardianUserId.Should().Be("guardian-id");
            offer.RespondedByGuardian.Should().BeTrue();
        }

        [Fact]
        public async Task GuardianFinalizeOfferAsync_DecliningOffer_UpdatesToDeclined()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                status: ScholarshipStatus.PendingGuardianSignature
            );
            offer.ExpirationDate = DateTime.UtcNow.AddDays(10);

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _service.GuardianFinalizeOfferAsync(1, "guardian-id", accept: false);

            // Assert
            offer.Status.Should().Be(ScholarshipStatus.Declined);
        }

        [Fact]
        public async Task GuardianFinalizeOfferAsync_WithExpiredOffer_ThrowsInvalidOperationException()
        {
            // Arrange
            var offer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                status: ScholarshipStatus.PendingGuardianSignature
            );
            offer.ExpirationDate = DateTime.UtcNow.AddDays(-5);

            _mockScholarshipRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(offer);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GuardianFinalizeOfferAsync(1, "guardian-id", accept: true)
            );

            offer.Status.Should().Be(ScholarshipStatus.Expired);
        }

        #endregion

        #region CheckExpirationsAsync Tests

        [Fact]
        public async Task CheckExpirationsAsync_WithExpiredOffers_ExpiresAndRefunds()
        {
            // Arrange
            var expiredOffer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 1,
                bandId: 1,
                amount: 5000,
                status: ScholarshipStatus.Sent
            );
            expiredOffer.ExpirationDate = DateTime.UtcNow.AddDays(-5);
            expiredOffer.CreatedAt = new DateTime(2025, 1, 1);

            var activeOffer = TestDataBuilder.CreateTestScholarshipOffer(
                id: 2,
                bandId: 1,
                amount: 3000,
                status: ScholarshipStatus.Sent
            );
            activeOffer.ExpirationDate = DateTime.UtcNow.AddDays(10);

            var budget = TestDataBuilder.CreateTestBandBudget(
                bandId: 1,
                fiscalYear: DateTime.UtcNow.Year,
                allocatedAmount: 8000,
                remainingAmount: 92000
            );

            var offers = new List<ScholarshipOffer> { expiredOffer, activeOffer }.AsQueryable().BuildMock();
            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            var budgets = new List<BandBudget> { budget }.AsQueryable().BuildMock();
            _mockBandBudgetRepo.Setup(r => r.GetQueryable()).Returns(budgets);

            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _service.CheckExpirationsAsync();

            // Assert
            expiredOffer.Status.Should().Be(ScholarshipStatus.Expired);
            activeOffer.Status.Should().Be(ScholarshipStatus.Sent); // Unchanged
            budget.AllocatedAmount.Should().Be(3000); // 8000 - 5000
            budget.RemainingAmount.Should().Be(97000); // 92000 + 5000
        }

        #endregion
    }
}
