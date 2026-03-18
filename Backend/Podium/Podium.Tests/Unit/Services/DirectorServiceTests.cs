using Xunit;
using Moq;
using FluentAssertions;
using Podium.Application.Services;
using Podium.Application.DTOs.BandStaff;
using Podium.Application.DTOs.Director;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.ScholarshipOffer;
using Podium.Core.Entities;
using Podium.Core.Constants;
using Podium.Core.Interfaces;
using Podium.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MockQueryable.Moq;

namespace Podium.Tests.Unit.Services
{
    public class DirectorServiceTests : TestBase
    {
        private readonly DirectorService _service;
        private readonly Mock<Podium.Core.Interfaces.IRepository<Band>> _mockBandRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<BandStaff>> _mockBandStaffRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<ScholarshipOffer>> _mockScholarshipRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<StudentInterest>> _mockStudentInterestRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<ContactRequest>> _mockContactRequestRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<BandEvent>> _mockBandEventRepo;
        private readonly Mock<INotificationService> _mockNotificationService;

        public DirectorServiceTests()
        {
            _mockBandRepo = new Mock<Podium.Core.Interfaces.IRepository<Band>>();
            _mockBandStaffRepo = new Mock<Podium.Core.Interfaces.IRepository<BandStaff>>();
            _mockScholarshipRepo = new Mock<Podium.Core.Interfaces.IRepository<ScholarshipOffer>>();
            _mockStudentInterestRepo = new Mock<Podium.Core.Interfaces.IRepository<StudentInterest>>();
            _mockContactRequestRepo = new Mock<Podium.Core.Interfaces.IRepository<ContactRequest>>();
            _mockBandEventRepo = new Mock<Podium.Core.Interfaces.IRepository<BandEvent>>();
            _mockNotificationService = new Mock<INotificationService>();

            MockUnitOfWork.Setup(u => u.Bands).Returns(_mockBandRepo.Object);
            MockUnitOfWork.Setup(u => u.BandStaff).Returns(_mockBandStaffRepo.Object);
            MockUnitOfWork.Setup(u => u.ScholarshipOffers).Returns(_mockScholarshipRepo.Object);
            MockUnitOfWork.Setup(u => u.StudentInterests).Returns(_mockStudentInterestRepo.Object);
            MockUnitOfWork.Setup(u => u.ContactRequests).Returns(_mockContactRequestRepo.Object);
            MockUnitOfWork.Setup(u => u.BandEvents).Returns(_mockBandEventRepo.Object);

            _service = new DirectorService(
                MockUnitOfWork.Object,
                MockLogger<DirectorService>().Object,
                _mockNotificationService.Object
            );
        }

        #region GetDashboardAsync Tests

        [Fact]
        public async Task GetDashboardAsync_WithValidDirector_ReturnsDashboardData()
        {
            // Arrange
            var directorUserId = "director-user-id";
            var bandId = 1;
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            var director = TestDataBuilder.CreateTestBandStaff(
                id: 1,
                bandId: bandId,
                role: "Director",
                applicationUserId: directorUserId
            );

            var band = TestDataBuilder.CreateTestBand(id: bandId);

            var directors = new List<BandStaff> { director }.AsQueryable().BuildMock();
            var bands = new List<Band> { band }.AsQueryable().BuildMock();
            var offers = new List<ScholarshipOffer>().AsQueryable().BuildMock();
            var contacts = new List<ContactRequest>().AsQueryable().BuildMock();

            _mockBandStaffRepo.Setup(r => r.GetQueryable()).Returns(directors);
            _mockBandRepo.Setup(r => r.GetQueryable()).Returns(bands);
            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);
            _mockContactRequestRepo.Setup(r => r.GetQueryable()).Returns(contacts);

            var filters = new DirectorDashboardFiltersDto { StartDate = startDate, EndDate = endDate };

            // Act
            var result = await _service.GetDashboardAsync(directorUserId, filters);

            // Assert
            result.Should().NotBeNull();
            result.KeyMetrics.Should().NotBeNull();
            result.RecruitmentFunnel.Should().NotBeNull();
            result.OffersOverview.Should().NotBeNull();
            result.StaffPerformance.Should().NotBeNull();
        }

        [Fact]
        public async Task GetDashboardAsync_WithInvalidDirector_ThrowsUnauthorizedException()
        {
            // Arrange
            var directorUserId = "invalid-user-id";
            var directors = new List<BandStaff>().AsQueryable().BuildMock();

            _mockBandStaffRepo.Setup(r => r.GetQueryable()).Returns(directors);

            var filters = new DirectorDashboardFiltersDto();

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetDashboardAsync(directorUserId, filters)
            );
        }

        #endregion

        #region GetKeyMetricsAsync Tests

        [Fact]
        public async Task GetKeyMetricsAsync_WithValidData_ReturnsMetrics()
        {
            // Arrange
            var directorUserId = "director-user-id";
            var bandId = 1;
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            var director = TestDataBuilder.CreateTestBandStaff(
                id: 1,
                bandId: bandId,
                role: "Director",
                applicationUserId: directorUserId
            );

            var offer1 = TestDataBuilder.CreateTestScholarshipOffer(id: 1, bandId: bandId, status: ScholarshipStatus.Accepted);
            offer1.CreatedAt = DateTime.UtcNow.AddDays(-10);

            var offer2 = TestDataBuilder.CreateTestScholarshipOffer(id: 2, bandId: bandId, status: ScholarshipStatus.Sent);
            offer2.CreatedAt = DateTime.UtcNow.AddDays(-5);

            var directors = new List<BandStaff> { director }.AsQueryable().BuildMock();
            var offers = new List<ScholarshipOffer> { offer1, offer2 }.AsQueryable().BuildMock();
            var staff = new List<BandStaff>().AsQueryable().BuildMock();
            var contacts = new List<ContactRequest>().AsQueryable().BuildMock();

            _mockBandStaffRepo.Setup(r => r.GetQueryable()).Returns(directors);
            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);
            _mockContactRequestRepo.Setup(r => r.GetQueryable()).Returns(contacts);

            // Act
            var result = await _service.GetKeyMetricsAsync(directorUserId, startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.TotalOffersSent.Should().Be(2);
            result.AcceptanceRate.Should().Be(50);
        }

        [Fact]
        public async Task GetKeyMetricsAsync_WithNoOffers_ReturnsZeroMetrics()
        {
            // Arrange
            var directorUserId = "director-user-id";
            var bandId = 1;
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            var director = TestDataBuilder.CreateTestBandStaff(
                id: 1,
                bandId: bandId,
                role: "Director",
                applicationUserId: directorUserId
            );

            var directors = new List<BandStaff> { director }.AsQueryable().BuildMock();
            var offers = new List<ScholarshipOffer>().AsQueryable().BuildMock();
            var staff = new List<BandStaff>().AsQueryable().BuildMock();
            var contacts = new List<ContactRequest>().AsQueryable().BuildMock();

            _mockBandStaffRepo.Setup(r => r.GetQueryable()).Returns(directors);
            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);
            _mockContactRequestRepo.Setup(r => r.GetQueryable()).Returns(contacts);

            // Act
            var result = await _service.GetKeyMetricsAsync(directorUserId, startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.TotalOffersSent.Should().Be(0);
            result.AcceptanceRate.Should().Be(0);
        }

        #endregion

        #region GetRecruitmentFunnelAsync Tests

        [Fact]
        public async Task GetRecruitmentFunnelAsync_WithValidData_ReturnsFunnelStages()
        {
            // Arrange
            var bandId = 1;
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            var contact1 = new ContactRequest
            {
                Id = 1,
                BandId = bandId,
                StudentId = 1,
                Status = "Approved",
                RequestedDate = DateTime.UtcNow.AddDays(-20)
            };

            var contact2 = new ContactRequest
            {
                Id = 2,
                BandId = bandId,
                StudentId = 2,
                Status = "Pending",
                RequestedDate = DateTime.UtcNow.AddDays(-15)
            };

            var offer1 = TestDataBuilder.CreateTestScholarshipOffer(id: 1, studentId: 1, bandId: bandId, status: ScholarshipStatus.Accepted);
            offer1.CreatedAt = DateTime.UtcNow.AddDays(-10);

            var contacts = new List<ContactRequest> { contact1, contact2 }.AsQueryable().BuildMock();
            var offers = new List<ScholarshipOffer> { offer1 }.AsQueryable().BuildMock();

            _mockContactRequestRepo.Setup(r => r.GetQueryable()).Returns(contacts);
            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            // Act
            var result = await _service.GetRecruitmentFunnelAsync(bandId, startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5);
            result[0].Stage.Should().Be("Contacted");
            result[1].Stage.Should().Be("Interested");
            result[2].Stage.Should().Be("Offered");
            result[3].Stage.Should().Be("Accepted");
            result[4].Stage.Should().Be("Enrolled");
        }

        [Fact]
        public async Task GetRecruitmentFunnelAsync_WithNoData_ReturnsEmptyFunnel()
        {
            // Arrange
            var bandId = 1;
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            var contacts = new List<ContactRequest>().AsQueryable().BuildMock();
            var offers = new List<ScholarshipOffer>().AsQueryable().BuildMock();

            _mockContactRequestRepo.Setup(r => r.GetQueryable()).Returns(contacts);
            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            // Act
            var result = await _service.GetRecruitmentFunnelAsync(bandId, startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5);
            result[0].Count.Should().Be(0);
        }

        #endregion

        #region GetOffersOverviewAsync Tests

        [Fact]
        public async Task GetOffersOverviewAsync_WithValidData_ReturnsOverview()
        {
            // Arrange
            var bandId = 1;
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            var student1 = TestDataBuilder.CreateTestStudent(id: 1, primaryInstrument: "Trumpet");
            var student2 = TestDataBuilder.CreateTestStudent(id: 2, primaryInstrument: "Saxophone");

            var staff = TestDataBuilder.CreateTestBandStaff(id: 1, bandId: bandId);

            var offer1 = TestDataBuilder.CreateTestScholarshipOffer(id: 1, studentId: 1, bandId: bandId, amount: 5000, status: ScholarshipStatus.Accepted);
            offer1.CreatedAt = DateTime.UtcNow.AddDays(-20);
            offer1.Student = student1;
            offer1.CreatedByStaff = staff;
            offer1.CreatedByStaffId = staff.Id;

            var offer2 = TestDataBuilder.CreateTestScholarshipOffer(id: 2, studentId: 2, bandId: bandId, amount: 3000, status: ScholarshipStatus.Sent);
            offer2.CreatedAt = DateTime.UtcNow.AddDays(-10);
            offer2.Student = student2;
            offer2.CreatedByStaff = staff;
            offer2.CreatedByStaffId = staff.Id;

            var offers = new List<ScholarshipOffer> { offer1, offer2 }.AsQueryable().BuildMock();

            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            // Act
            var result = await _service.GetOffersOverviewAsync(bandId, startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.TotalOffers.Should().Be(2);
            result.AcceptedOffers.Should().Be(1);
            result.OffersByInstrument.Should().HaveCount(2);
            result.OffersByStatus.Should().NotBeEmpty();
        }

        #endregion

        #region GetStaffPerformanceAsync Tests

        [Fact]
        public async Task GetStaffPerformanceAsync_WithValidData_ReturnsPerformanceList()
        {
            // Arrange
            var bandId = 1;
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            var user = TestDataBuilder.CreateTestApplicationUser(id: "staff-user-1", email: "staff@test.com");

            var staffMember = TestDataBuilder.CreateTestBandStaff(
                id: 1,
                bandId: bandId,
                firstName: "John",
                lastName: "Recruiter",
                role: "Recruiter",
                applicationUserId: "staff-user-1"
            );
            staffMember.ApplicationUser = user;
            staffMember.LastActivityDate = DateTime.UtcNow.AddDays(-1);

            var offer = TestDataBuilder.CreateTestScholarshipOffer(id: 1, bandId: bandId, status: ScholarshipStatus.Accepted);
            offer.CreatedAt = DateTime.UtcNow.AddDays(-10);
            offer.CreatedByStaffId = 1;

            var contact = new ContactRequest
            {
                Id = 1,
                BandId = bandId,
                BandStaffId = 1,
                StudentId = 1,
                Status = "Approved",
                RequestedDate = DateTime.UtcNow.AddDays(-15)
            };

            var staff = new List<BandStaff> { staffMember }.AsQueryable().BuildMock();
            var offers = new List<ScholarshipOffer> { offer }.AsQueryable().BuildMock();
            var contacts = new List<ContactRequest> { contact }.AsQueryable().BuildMock();

            _mockBandStaffRepo.Setup(r => r.GetQueryable()).Returns(staff);
            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);
            _mockContactRequestRepo.Setup(r => r.GetQueryable()).Returns(contacts);

            // Act
            var result = await _service.GetStaffPerformanceAsync(bandId, startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].StaffName.Should().Be("John Recruiter");
            result[0].OffersCreated.Should().Be(1);
            result[0].StudentsContacted.Should().Be(1);
        }

        #endregion

        #region AddStaffMemberAsync Tests

        [Fact]
        public async Task AddStaffMemberAsync_WithValidData_AddsStaffSuccessfully()
        {
            // Arrange
            var directorUserId = "director-user-id";
            var bandId = 1;

            var band = TestDataBuilder.CreateTestBand(id: bandId, director: directorUserId);

            var request = new CreateBandStaffDto
            {
                BandId = bandId,
                ApplicationUserId = "new-staff-user-id",
                Role = "Recruiter",
                CanContact = true,
                CanMakeOffers = false,
                CanViewFinancials = false
            };

            var bands = new List<Band> { band }.AsQueryable().BuildMock();
            var existingStaff = new List<BandStaff>().AsQueryable().BuildMock();

            _mockBandRepo.Setup(r => r.GetQueryable()).Returns(bands);
            _mockBandStaffRepo.Setup(r => r.GetQueryable()).Returns(existingStaff);
            _mockBandStaffRepo.Setup(r => r.AddAsync(It.IsAny<BandStaff>())).Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.AddStaffMemberAsync(directorUserId, request);

            // Assert
            result.Should().NotBeNull();
            result.Role.Should().Be("Recruiter");
            _mockBandStaffRepo.Verify(r => r.AddAsync(It.IsAny<BandStaff>()), Times.Once);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AddStaffMemberAsync_WithUnauthorizedDirector_ThrowsUnauthorizedException()
        {
            // Arrange
            var directorUserId = "director-user-id";
            var bandId = 1;

            var request = new CreateBandStaffDto
            {
                BandId = bandId,
                ApplicationUserId = "new-staff-user-id",
                Role = "Recruiter"
            };

            var bands = new List<Band>().AsQueryable().BuildMock();

            _mockBandRepo.Setup(r => r.GetQueryable()).Returns(bands);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AddStaffMemberAsync(directorUserId, request)
            );
        }

        [Fact]
        public async Task AddStaffMemberAsync_WithExistingActiveStaff_ThrowsInvalidOperationException()
        {
            // Arrange
            var directorUserId = "director-user-id";
            var bandId = 1;

            var band = TestDataBuilder.CreateTestBand(id: bandId, director: directorUserId);

            var existingStaffMember = TestDataBuilder.CreateTestBandStaff(
                id: 1,
                bandId: bandId,
                applicationUserId: "existing-staff-user-id"
            );
            existingStaffMember.IsActive = true;

            var request = new CreateBandStaffDto
            {
                BandId = bandId,
                ApplicationUserId = "existing-staff-user-id",
                Role = "Recruiter"
            };

            var bands = new List<Band> { band }.AsQueryable().BuildMock();
            var staff = new List<BandStaff> { existingStaffMember }.AsQueryable().BuildMock();

            _mockBandRepo.Setup(r => r.GetQueryable()).Returns(bands);
            _mockBandStaffRepo.Setup(r => r.GetQueryable()).Returns(staff);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddStaffMemberAsync(directorUserId, request)
            );
        }

        [Fact]
        public async Task AddStaffMemberAsync_WithInactiveExistingStaff_ReactivatesStaff()
        {
            // Arrange
            var directorUserId = "director-user-id";
            var bandId = 1;

            var band = TestDataBuilder.CreateTestBand(id: bandId, director: directorUserId);

            var existingStaffMember = TestDataBuilder.CreateTestBandStaff(
                id: 1,
                bandId: bandId,
                applicationUserId: "existing-staff-user-id"
            );
            existingStaffMember.IsActive = false;

            var request = new CreateBandStaffDto
            {
                BandId = bandId,
                ApplicationUserId = "existing-staff-user-id",
                Role = "Recruiter",
                CanContact = true,
                CanMakeOffers = true
            };

            var bands = new List<Band> { band }.AsQueryable().BuildMock();
            var staff = new List<BandStaff> { existingStaffMember }.AsQueryable().BuildMock();

            _mockBandRepo.Setup(r => r.GetQueryable()).Returns(bands);
            _mockBandStaffRepo.Setup(r => r.GetQueryable()).Returns(staff);
            _mockBandStaffRepo.Setup(r => r.Update(It.IsAny<BandStaff>()));
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.AddStaffMemberAsync(directorUserId, request);

            // Assert
            result.Should().NotBeNull();
            result.IsActive.Should().BeTrue();
            _mockBandStaffRepo.Verify(r => r.Update(It.Is<BandStaff>(s => s.IsActive == true)), Times.Once);
        }

        #endregion

        #region UpdateStaffMemberAsync Tests

        [Fact]
        public async Task UpdateStaffMemberAsync_WithValidData_UpdatesStaffSuccessfully()
        {
            // Arrange
            var staffId = 1;
            var staffMember = TestDataBuilder.CreateTestBandStaff(id: staffId, role: "Recruiter");

            var request = new UpdateBandStaffDto
            {
                Role = "Senior Recruiter",
                CanContact = true,
                CanMakeOffers = true,
                CanViewFinancials = true,
                IsActive = true
            };

            _mockBandStaffRepo.Setup(r => r.GetByIdAsync(staffId)).ReturnsAsync(staffMember);
            _mockBandStaffRepo.Setup(r => r.Update(It.IsAny<BandStaff>()));
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.UpdateStaffMemberAsync(staffId, request);

            // Assert
            result.Should().NotBeNull();
            _mockBandStaffRepo.Verify(r => r.Update(It.Is<BandStaff>(s => s.Role == "Senior Recruiter")), Times.Once);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStaffMemberAsync_WithInvalidStaffId_ThrowsKeyNotFoundException()
        {
            // Arrange
            var staffId = 999;
            var request = new UpdateBandStaffDto { Role = "Recruiter" };

            _mockBandStaffRepo.Setup(r => r.GetByIdAsync(staffId)).ReturnsAsync((BandStaff?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateStaffMemberAsync(staffId, request)
            );
        }

        #endregion

        #region RemoveStaffMemberAsync Tests

        [Fact]
        public async Task RemoveStaffMemberAsync_WithValidStaffId_DeactivatesStaff()
        {
            // Arrange
            var staffId = 1;
            var staffMember = TestDataBuilder.CreateTestBandStaff(id: staffId);
            staffMember.IsActive = true;

            _mockBandStaffRepo.Setup(r => r.GetByIdAsync(staffId)).ReturnsAsync(staffMember);
            _mockBandStaffRepo.Setup(r => r.Update(It.IsAny<BandStaff>()));
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _service.RemoveStaffMemberAsync(staffId);

            // Assert
            _mockBandStaffRepo.Verify(r => r.Update(It.Is<BandStaff>(s => s.IsActive == false)), Times.Once);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RemoveStaffMemberAsync_WithInvalidStaffId_ThrowsKeyNotFoundException()
        {
            // Arrange
            var staffId = 999;

            _mockBandStaffRepo.Setup(r => r.GetByIdAsync(staffId)).ReturnsAsync((BandStaff?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.RemoveStaffMemberAsync(staffId)
            );
        }

        #endregion

        #region GetScholarshipsAsync Tests

        [Fact]
        public async Task GetScholarshipsAsync_WithValidFilters_ReturnsScholarships()
        {
            // Arrange
            var userId = "director-user-id";
            var bandId = 1;

            var band = TestDataBuilder.CreateTestBand(id: bandId, director: userId);
            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var createdByStaff = TestDataBuilder.CreateTestBandStaff(id: 1, bandId: bandId);

            var offer1 = TestDataBuilder.CreateTestScholarshipOffer(id: 1, studentId: 1, bandId: bandId, amount: 5000, status: ScholarshipStatus.Sent);
            offer1.Student = student;
            offer1.Band = band;
            offer1.CreatedByStaff = createdByStaff;
            offer1.CreatedByStaffId = 1;

            var filters = new ScholarshipFilterDto
            {
                Page = 1,
                PageSize = 10,
                Status = "Sent"
            };

            var bands = new List<Band> { band }.AsQueryable().BuildMock();
            var offers = new List<ScholarshipOffer> { offer1 }.AsQueryable().BuildMock();

            _mockBandRepo.Setup(r => r.GetQueryable()).Returns(bands);
            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            // Act
            var result = await _service.GetScholarshipsAsync(userId, filters);

            // Assert
            result.Should().NotBeNull();
            result.TotalOffers.Should().Be(1);
            result.Offers.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetScholarshipsAsync_WithInvalidUserId_ThrowsKeyNotFoundException()
        {
            // Arrange
            var userId = "invalid-user-id";
            var filters = new ScholarshipFilterDto { Page = 1, PageSize = 10 };

            var bands = new List<Band>().AsQueryable().BuildMock();

            _mockBandRepo.Setup(r => r.GetQueryable()).Returns(bands);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetScholarshipsAsync(userId, filters)
            );
        }

        #endregion

        #region ApproveScholarshipAsync Tests

        [Fact]
        public async Task ApproveScholarshipAsync_WithValidOffer_ApprovesSuccessfully()
        {
            // Arrange
            var offerId = 1;
            var userId = "director-user-id";
            var notes = "Approved by director";

            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var band = TestDataBuilder.CreateTestBand(id: 1);
            var staff = TestDataBuilder.CreateTestBandStaff(id: 1, bandId: 1);

            var offer = TestDataBuilder.CreateTestScholarshipOffer(id: offerId, status: ScholarshipStatus.Pending);
            offer.Student = student;
            offer.Band = band;
            offer.CreatedByStaff = staff;

            var offers = new List<ScholarshipOffer> { offer }.AsQueryable().BuildMock();

            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);
            _mockScholarshipRepo.Setup(r => r.Update(It.IsAny<ScholarshipOffer>()));
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ApproveScholarshipAsync(offerId, userId, notes);

            // Assert
            result.Should().NotBeNull();
            _mockScholarshipRepo.Verify(r => r.Update(It.Is<ScholarshipOffer>(
                o => o.Status == ScholarshipStatus.Accepted && o.ApprovedByUserId == userId
            )), Times.Once);
        }

        [Fact]
        public async Task ApproveScholarshipAsync_WithInvalidOfferId_ThrowsKeyNotFoundException()
        {
            // Arrange
            var offerId = 999;
            var userId = "director-user-id";

            var offers = new List<ScholarshipOffer>().AsQueryable().BuildMock();

            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.ApproveScholarshipAsync(offerId, userId, null)
            );
        }

        [Fact]
        public async Task ApproveScholarshipAsync_WithNonPendingStatus_ThrowsInvalidOperationException()
        {
            // Arrange
            var offerId = 1;
            var userId = "director-user-id";

            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var band = TestDataBuilder.CreateTestBand(id: 1);
            var staff = TestDataBuilder.CreateTestBandStaff(id: 1, bandId: 1);

            var offer = TestDataBuilder.CreateTestScholarshipOffer(id: offerId, status: ScholarshipStatus.Accepted);
            offer.Student = student;
            offer.Band = band;
            offer.CreatedByStaff = staff;

            var offers = new List<ScholarshipOffer> { offer }.AsQueryable().BuildMock();

            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApproveScholarshipAsync(offerId, userId, null)
            );
        }

        #endregion

        #region RescindScholarshipAsync Tests

        [Fact]
        public async Task RescindScholarshipAsync_WithValidOffer_RescindsSuccessfully()
        {
            // Arrange
            var offerId = 1;
            var userId = "director-user-id";
            var reason = "Budget constraints";

            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var band = TestDataBuilder.CreateTestBand(id: 1);
            var staff = TestDataBuilder.CreateTestBandStaff(id: 1, bandId: 1);

            var offer = TestDataBuilder.CreateTestScholarshipOffer(id: offerId, status: ScholarshipStatus.Sent);
            offer.Student = student;
            offer.Band = band;
            offer.CreatedByStaff = staff;

            var offers = new List<ScholarshipOffer> { offer }.AsQueryable().BuildMock();

            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);
            _mockScholarshipRepo.Setup(r => r.Update(It.IsAny<ScholarshipOffer>()));
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.RescindScholarshipAsync(offerId, userId, reason);

            // Assert
            result.Should().NotBeNull();
            _mockScholarshipRepo.Verify(r => r.Update(It.Is<ScholarshipOffer>(
                o => o.Status == ScholarshipStatus.Rescinded && o.RescindReason == reason
            )), Times.Once);
        }

        [Fact]
        public async Task RescindScholarshipAsync_WithInvalidOfferId_ThrowsKeyNotFoundException()
        {
            // Arrange
            var offerId = 999;
            var userId = "director-user-id";
            var reason = "Budget constraints";

            var offers = new List<ScholarshipOffer>().AsQueryable().BuildMock();

            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.RescindScholarshipAsync(offerId, userId, reason)
            );
        }

        [Fact]
        public async Task RescindScholarshipAsync_WithAlreadyRescindedOffer_ThrowsInvalidOperationException()
        {
            // Arrange
            var offerId = 1;
            var userId = "director-user-id";
            var reason = "Budget constraints";

            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var band = TestDataBuilder.CreateTestBand(id: 1);
            var staff = TestDataBuilder.CreateTestBandStaff(id: 1, bandId: 1);

            var offer = TestDataBuilder.CreateTestScholarshipOffer(id: offerId, status: ScholarshipStatus.Rescinded);
            offer.Student = student;
            offer.Band = band;
            offer.CreatedByStaff = staff;

            var offers = new List<ScholarshipOffer> { offer }.AsQueryable().BuildMock();

            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RescindScholarshipAsync(offerId, userId, reason)
            );
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task CanAccessBandAsync_WithValidDirector_ReturnsTrue()
        {
            // Arrange
            var userId = "director-user-id";
            var bandId = 1;

            var band = TestDataBuilder.CreateTestBand(id: bandId, director: userId);
            var bands = new List<Band> { band }.AsQueryable().BuildMock();

            _mockBandRepo.Setup(r => r.GetQueryable()).Returns(bands);

            // Act
            var result = await _service.CanAccessBandAsync(userId, bandId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanAccessBandAsync_WithInvalidDirector_ReturnsFalse()
        {
            // Arrange
            var userId = "invalid-user-id";
            var bandId = 1;

            var band = TestDataBuilder.CreateTestBand(id: bandId, director: "other-director");
            var bands = new List<Band> { band }.AsQueryable().BuildMock();

            _mockBandRepo.Setup(r => r.GetQueryable()).Returns(bands);

            // Act
            var result = await _service.CanAccessBandAsync(userId, bandId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CanManageStaffAsync_WithValidDirector_ReturnsTrue()
        {
            // Arrange
            var userId = "director-user-id";
            var staffId = 1;

            var band = TestDataBuilder.CreateTestBand(id: 1, director: userId);
            var staffMember = TestDataBuilder.CreateTestBandStaff(id: staffId, bandId: 1);
            staffMember.Band = band;

            var staff = new List<BandStaff> { staffMember }.AsQueryable().BuildMock();

            _mockBandStaffRepo.Setup(r => r.GetQueryable()).Returns(staff);

            // Act
            var result = await _service.CanManageStaffAsync(userId, staffId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanManageScholarshipAsync_WithValidDirector_ReturnsTrue()
        {
            // Arrange
            var userId = "director-user-id";
            var offerId = 1;

            var band = TestDataBuilder.CreateTestBand(id: 1, director: userId);
            var offer = TestDataBuilder.CreateTestScholarshipOffer(id: offerId, bandId: 1);
            offer.Band = band;

            var offers = new List<ScholarshipOffer> { offer }.AsQueryable().BuildMock();

            _mockScholarshipRepo.Setup(r => r.GetQueryable()).Returns(offers);

            // Act
            var result = await _service.CanManageScholarshipAsync(userId, offerId);

            // Assert
            result.Should().BeTrue();
        }

        #endregion
    }
}
