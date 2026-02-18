using Xunit;
using Moq;
using FluentAssertions;
using Podium.Application.Services;
using Podium.Application.DTOs.Student;
using Podium.Application.DTOs.Rating;
using Podium.Application.DTOs;
using Podium.Core.Entities;
using Podium.Core.Constants;
using Podium.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MockQueryable.Moq;

namespace Podium.Tests.Unit.Services
{
    public class StudentServiceTests : TestBase
    {
        private readonly StudentService _service;
        private readonly Mock<Podium.Core.Interfaces.IRepository<Student>> _mockStudentRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<Band>> _mockBandRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<StudentInterest>> _mockStudentInterestRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<BandStaff>> _mockBandStaffRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<StudentRating>> _mockStudentRatingRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<Guardian>> _mockGuardianRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<StudentGuardian>> _mockStudentGuardianRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<ScholarshipOffer>> _mockScholarshipOfferRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<ContactRequest>> _mockContactRequestRepo;
        private readonly Mock<Podium.Core.Interfaces.IRepository<Notification>> _mockNotificationRepo;

        public StudentServiceTests()
        {
            _mockStudentRepo = new Mock<Podium.Core.Interfaces.IRepository<Student>>();
            _mockBandRepo = new Mock<Podium.Core.Interfaces.IRepository<Band>>();
            _mockStudentInterestRepo = new Mock<Podium.Core.Interfaces.IRepository<StudentInterest>>();
            _mockBandStaffRepo = new Mock<Podium.Core.Interfaces.IRepository<BandStaff>>();
            _mockStudentRatingRepo = new Mock<Podium.Core.Interfaces.IRepository<StudentRating>>();
            _mockGuardianRepo = new Mock<Podium.Core.Interfaces.IRepository<Guardian>>();
            _mockStudentGuardianRepo = new Mock<Podium.Core.Interfaces.IRepository<StudentGuardian>>();
            _mockScholarshipOfferRepo = new Mock<Podium.Core.Interfaces.IRepository<ScholarshipOffer>>();
            _mockContactRequestRepo = new Mock<Podium.Core.Interfaces.IRepository<ContactRequest>>();
            _mockNotificationRepo = new Mock<Podium.Core.Interfaces.IRepository<Notification>>();

            MockUnitOfWork.Setup(u => u.Students).Returns(_mockStudentRepo.Object);
            MockUnitOfWork.Setup(u => u.Bands).Returns(_mockBandRepo.Object);
            MockUnitOfWork.Setup(u => u.StudentInterests).Returns(_mockStudentInterestRepo.Object);
            MockUnitOfWork.Setup(u => u.BandStaff).Returns(_mockBandStaffRepo.Object);
            MockUnitOfWork.Setup(u => u.StudentRatings).Returns(_mockStudentRatingRepo.Object);
            MockUnitOfWork.Setup(u => u.Guardians).Returns(_mockGuardianRepo.Object);
            MockUnitOfWork.Setup(u => u.StudentGuardians).Returns(_mockStudentGuardianRepo.Object);
            MockUnitOfWork.Setup(u => u.ScholarshipOffers).Returns(_mockScholarshipOfferRepo.Object);
            MockUnitOfWork.Setup(u => u.ContactRequests).Returns(_mockContactRequestRepo.Object);
            MockUnitOfWork.Setup(u => u.Notifications).Returns(_mockNotificationRepo.Object);

            _service = new StudentService(
                MockUnitOfWork.Object,
                MockPermissionService.Object,
                MockNotificationService.Object,
                MockEmailService.Object
            );
        }

        #region GetStudentDetailsAsync Tests

        [Fact]
        public async Task GetStudentDetailsAsync_WithValidIdAndPermission_ReturnsStudentDetails()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var students = new List<Student> { student }.AsQueryable().BuildMock();
            
            _mockStudentRepo.Setup(r => r.GetQueryable()).Returns(students);
            MockPermissionService.Setup(p => p.GetCurrentUserRoleAsync()).ReturnsAsync(Roles.Director);
            MockPermissionService.Setup(p => p.HasPermissionAsync(Permissions.ViewStudents)).ReturnsAsync(true);

            // Act
            var result = await _service.GetStudentDetailsAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.StudentId.Should().Be(1);
            result.Data.FirstName.Should().Be("John");
            result.Data.LastName.Should().Be("Doe");
        }

        [Fact]
        public async Task GetStudentDetailsAsync_WithNonExistentStudent_ReturnsFailure()
        {
            // Arrange
            var students = new List<Student>().AsQueryable().BuildMock();
            _mockStudentRepo.Setup(r => r.GetQueryable()).Returns(students);

            // Act
            var result = await _service.GetStudentDetailsAsync(999);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Student not found");
        }

        [Fact]
        public async Task GetStudentDetailsAsync_WithoutPermission_ReturnsForbidden()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var students = new List<Student> { student }.AsQueryable().BuildMock();
            
            _mockStudentRepo.Setup(r => r.GetQueryable()).Returns(students);
            MockPermissionService.Setup(p => p.GetCurrentUserRoleAsync()).ReturnsAsync(Roles.Student);
            MockPermissionService.Setup(p => p.IsStudentOwnerAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _service.GetStudentDetailsAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("You don't have permission to view this student");
        }

        [Fact]
        public async Task GetStudentDetailsAsync_AsStudentOwner_ReturnsDetails()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var students = new List<Student> { student }.AsQueryable().BuildMock();
            
            _mockStudentRepo.Setup(r => r.GetQueryable()).Returns(students);
            MockPermissionService.Setup(p => p.GetCurrentUserRoleAsync()).ReturnsAsync(Roles.Student);
            MockPermissionService.Setup(p => p.IsStudentOwnerAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _service.GetStudentDetailsAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        #endregion

        #region UpdateStudentProfileAsync Tests

        [Fact]
        public async Task UpdateStudentProfileAsync_AsOwner_UpdatesSuccessfully()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var dto = new UpdateStudentDto
            {
                FirstName = "Jane",
                LastName = "Smith",
                PhoneNumber = "555-1234",
                GraduationYear = 2026,
                GPA = 3.8m,
                PrimaryInstrument = "Clarinet",
                SecondaryInstruments = new List<string> { "Saxophone" },
                Achievements = new List<string> { "State Champion" }
            };

            _mockStudentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
            MockPermissionService.Setup(p => p.IsStudentOwnerAsync(1)).ReturnsAsync(true);
            _mockStudentRepo.Setup(r => r.Update(It.IsAny<Student>()));
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.UpdateStudentProfileAsync(1, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            student.FirstName.Should().Be("Jane");
            student.LastName.Should().Be("Smith");
            student.PrimaryInstrument.Should().Be("Clarinet");
            _mockStudentRepo.Verify(r => r.Update(It.IsAny<Student>()), Times.Once);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStudentProfileAsync_WithNonExistentStudent_ReturnsFailure()
        {
            // Arrange
            var dto = new UpdateStudentDto { FirstName = "Jane", LastName = "Smith" };
            _mockStudentRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student)null);

            // Act
            var result = await _service.UpdateStudentProfileAsync(999, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Student not found");
        }

        [Fact]
        public async Task UpdateStudentProfileAsync_WithoutOwnership_ReturnsForbidden()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var dto = new UpdateStudentDto { FirstName = "Jane", LastName = "Smith" };
            
            _mockStudentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
            MockPermissionService.Setup(p => p.IsStudentOwnerAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _service.UpdateStudentProfileAsync(1, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("You can only update your own profile");
        }

        #endregion

        #region ShowInterestAsync Tests

        [Fact]
        public async Task ShowInterestAsync_WithValidData_CreatesInterestAndNotifies()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var band = TestDataBuilder.CreateTestBand(id: 1);
            var bandStaff = TestDataBuilder.CreateTestBandStaff(id: 1, bandId: 1);
            bandStaff.CanViewStudents = true;
            bandStaff.ApplicationUser = TestDataBuilder.CreateTestApplicationUser(id: "staff-1", email: "staff@test.com");

            var existingInterests = new List<StudentInterest>().AsQueryable();
            var bandStaffList = new List<BandStaff> { bandStaff }.AsQueryable().BuildMock();
            var studentGuardians = new List<StudentGuardian>().AsQueryable().BuildMock();

            MockPermissionService.Setup(p => p.IsStudentOwnerAsync(1)).ReturnsAsync(true);
            _mockStudentInterestRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentInterest, bool>>>()))
                .ReturnsAsync(existingInterests.ToList());
            _mockStudentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
            _mockBandRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(band);
            _mockStudentInterestRepo.Setup(r => r.AddAsync(It.IsAny<StudentInterest>())).Returns(Task.CompletedTask);
            _mockBandStaffRepo.Setup(r => r.GetQueryable()).Returns(bandStaffList);
            _mockStudentGuardianRepo.Setup(r => r.GetQueryable()).Returns(studentGuardians);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ShowInterestAsync(1, 1);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockStudentInterestRepo.Verify(r => r.AddAsync(It.IsAny<StudentInterest>()), Times.Once);
            // Service notifies band staff twice: once at line 140 and again at line 177 in StudentService
            MockNotificationService.Verify(n => n.NotifyBandStaffAsync(
                1, "NewInterest", "New Student Interest", 
                It.IsAny<string>(), "1", It.IsAny<NotificationPriority>()), Times.Exactly(2));
            MockEmailService.Verify(e => e.SendEmailAsync(
                "staff@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ShowInterestAsync_WithoutOwnership_ReturnsForbidden()
        {
            // Arrange
            MockPermissionService.Setup(p => p.IsStudentOwnerAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _service.ShowInterestAsync(1, 1);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("You can only express interest for your own profile");
        }

        [Fact]
        public async Task ShowInterestAsync_WithExistingInterest_ReturnsFailure()
        {
            // Arrange
            var existingInterest = new StudentInterest { StudentId = 1, BandId = 1 };
            var existingInterests = new List<StudentInterest> { existingInterest };

            MockPermissionService.Setup(p => p.IsStudentOwnerAsync(1)).ReturnsAsync(true);
            _mockStudentInterestRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentInterest, bool>>>()))
                .ReturnsAsync(existingInterests);

            // Act
            var result = await _service.ShowInterestAsync(1, 1);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("You have already shown interest in this band");
        }

        [Fact]
        public async Task ShowInterestAsync_WithNonExistentStudent_ReturnsFailure()
        {
            // Arrange
            var existingInterests = new List<StudentInterest>();
            
            MockPermissionService.Setup(p => p.IsStudentOwnerAsync(1)).ReturnsAsync(true);
            _mockStudentInterestRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentInterest, bool>>>()))
                .ReturnsAsync(existingInterests);
            _mockStudentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Student)null);
            _mockBandRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataBuilder.CreateTestBand());

            // Act
            var result = await _service.ShowInterestAsync(1, 1);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Student or Band not found.");
        }

        [Fact]
        public async Task ShowInterestAsync_NotifiesGuardians_WhenOptedIn()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var band = TestDataBuilder.CreateTestBand(id: 1);
            var guardian = TestDataBuilder.CreateTestGuardian(id: 1);
            guardian.EmailNotificationsEnabled = true;
            guardian.ApplicationUserId = "guardian-1";

            var studentGuardian = new StudentGuardian 
            { 
                StudentId = 1, 
                GuardianId = 1, 
                IsActive = true, 
                ReceivesNotifications = true,
                Guardian = guardian
            };

            var existingInterests = new List<StudentInterest>();
            var studentGuardians = new List<StudentGuardian> { studentGuardian }.AsQueryable().BuildMock();
            var bandStaffList = new List<BandStaff>().AsQueryable().BuildMock();

            MockPermissionService.Setup(p => p.IsStudentOwnerAsync(1)).ReturnsAsync(true);
            _mockStudentInterestRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentInterest, bool>>>()))
                .ReturnsAsync(existingInterests);
            _mockStudentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
            _mockBandRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(band);
            _mockStudentInterestRepo.Setup(r => r.AddAsync(It.IsAny<StudentInterest>())).Returns(Task.CompletedTask);
            _mockStudentGuardianRepo.Setup(r => r.GetQueryable()).Returns(studentGuardians);
            _mockBandStaffRepo.Setup(r => r.GetQueryable()).Returns(bandStaffList);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ShowInterestAsync(1, 1);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            // Service notifies guardians twice: once at line 150-169 and again at line 210-231 in StudentService
            MockNotificationService.Verify(n => n.NotifyUserAsync(
                "guardian-1", "StudentActivity", "Student Interest Updated", 
                It.IsAny<string>(), "1", It.IsAny<NotificationPriority>(), It.IsAny<DateTime?>()), Times.Exactly(2));
            MockEmailService.Verify(e => e.SendEmailAsync(
                guardian.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region RateStudentAsync Tests

        [Fact]
        public async Task RateStudentAsync_WithPermission_CreatesRatingSuccessfully()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var bandStaff = TestDataBuilder.CreateTestBandStaff(id: 1);
            var dto = new RatingDto { Rating = 5, Comments = "Excellent performance" };

            MockPermissionService.Setup(p => p.HasPermissionAsync(Permissions.RateStudents)).ReturnsAsync(true);
            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync("staff-user-id");
            _mockStudentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
            _mockBandStaffRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BandStaff, bool>>>()))
                .ReturnsAsync(new List<BandStaff> { bandStaff });
            _mockStudentRatingRepo.Setup(r => r.AddAsync(It.IsAny<StudentRating>())).Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.RateStudentAsync(1, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockStudentRatingRepo.Verify(r => r.AddAsync(It.Is<StudentRating>(sr => 
                sr.StudentId == 1 && sr.Rating == 5 && sr.Comments == "Excellent performance")), Times.Once);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RateStudentAsync_WithoutPermission_ReturnsForbidden()
        {
            // Arrange
            var dto = new RatingDto { Rating = 4 };
            MockPermissionService.Setup(p => p.HasPermissionAsync(Permissions.RateStudents)).ReturnsAsync(false);

            // Act
            var result = await _service.RateStudentAsync(1, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("You do not have permission to rate students.");
        }

        [Fact]
        public async Task RateStudentAsync_WithNonExistentStudent_ReturnsFailure()
        {
            // Arrange
            var dto = new RatingDto { Rating = 3 };
            MockPermissionService.Setup(p => p.HasPermissionAsync(Permissions.RateStudents)).ReturnsAsync(true);
            _mockStudentRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student)null);

            // Act
            var result = await _service.RateStudentAsync(999, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Student not found.");
        }

        [Fact]
        public async Task RateStudentAsync_WithNonExistentBandStaff_ReturnsFailure()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var dto = new RatingDto { Rating = 5 };

            MockPermissionService.Setup(p => p.HasPermissionAsync(Permissions.RateStudents)).ReturnsAsync(true);
            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync("unknown-user");
            _mockStudentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
            _mockBandStaffRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BandStaff, bool>>>()))
                .ReturnsAsync(new List<BandStaff>());

            // Act
            var result = await _service.RateStudentAsync(1, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Rater profile not found.");
        }

        #endregion

        #region GetStudentCardsAsync Tests

        [Fact]
        public async Task GetStudentCardsAsync_WithPermission_ReturnsPagedStudents()
        {
            // Arrange
            var students = new List<Student>
            {
                TestDataBuilder.CreateTestStudent(id: 1, primaryInstrument: "Trumpet"),
                TestDataBuilder.CreateTestStudent(id: 2, primaryInstrument: "Clarinet")
            };

            MockPermissionService.Setup(p => p.HasPermissionAsync(Permissions.ViewStudents)).ReturnsAsync(true);
            _mockStudentRepo.Setup(r => r.GetPagedProjectionAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Student, StudentCardDto>>>(),
                1, 10,
                It.IsAny<System.Linq.Expressions.Expression<Func<Student, object>>>()))
                .ReturnsAsync(new PagedResult<StudentCardDto>
                {
                    Items = students.Select(s => new StudentCardDto 
                    { 
                        Id = s.Id, 
                        FirstName = s.FirstName,
                        LastName = s.LastName,
                        Instrument = s.PrimaryInstrument
                    }),
                    TotalCount = 2,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await _service.GetStudentCardsAsync(null, null, 1, 10);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetStudentCardsAsync_WithInstrumentFilter_ReturnsFilteredStudents()
        {
            // Arrange
            MockPermissionService.Setup(p => p.HasPermissionAsync(Permissions.ViewStudents)).ReturnsAsync(true);
            _mockStudentRepo.Setup(r => r.GetPagedProjectionAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Student, StudentCardDto>>>(),
                1, 10,
                It.IsAny<System.Linq.Expressions.Expression<Func<Student, object>>>()))
                .ReturnsAsync(new PagedResult<StudentCardDto>
                {
                    Items = new List<StudentCardDto> 
                    { 
                        new StudentCardDto { Id = 1, Instrument = "Trumpet" }
                    },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await _service.GetStudentCardsAsync("Trumpet", null, 1, 10);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetStudentCardsAsync_WithoutPermission_ReturnsForbidden()
        {
            // Arrange
            MockPermissionService.Setup(p => p.HasPermissionAsync(Permissions.ViewStudents)).ReturnsAsync(false);

            // Act
            var result = await _service.GetStudentCardsAsync(null, null, 1, 10);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("No permission to view students");
        }

        #endregion

        #region GetAccessibleStudentsAsync Tests

        [Fact]
        public async Task GetAccessibleStudentsAsync_AsStudent_ReturnsOwnProfile()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1, applicationUserId: "student-user-id");
            var students = new List<Student> { student }.AsQueryable().BuildMock();

            MockPermissionService.Setup(p => p.GetCurrentUserRoleAsync()).ReturnsAsync(Roles.Student);
            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync("student-user-id");
            _mockStudentRepo.Setup(r => r.GetQueryable()).Returns(students);

            // Act
            var result = await _service.GetAccessibleStudentsAsync(1, 10);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().StudentId.Should().Be(1);
        }

        [Fact]
        public async Task GetAccessibleStudentsAsync_AsGuardian_ReturnsLinkedStudents()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1);
            var guardian = TestDataBuilder.CreateTestGuardian(id: 1, applicationUserId: "guardian-user-id");
            guardian.Students.Add(student);

            var guardians = new List<Guardian> { guardian }.AsQueryable().BuildMock();
            var students = new List<Student> { student }.AsQueryable().BuildMock();

            MockPermissionService.Setup(p => p.GetCurrentUserRoleAsync()).ReturnsAsync(Roles.Guardian);
            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync("guardian-user-id");
            _mockGuardianRepo.Setup(r => r.GetQueryable()).Returns(guardians);
            _mockStudentRepo.Setup(r => r.GetQueryable()).Returns(students);

            // Act
            var result = await _service.GetAccessibleStudentsAsync(1, 10);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAccessibleStudentsAsync_AsDirectorWithPermission_ReturnsAllStudents()
        {
            // Arrange
            var students = new List<Student>
            {
                TestDataBuilder.CreateTestStudent(id: 1),
                TestDataBuilder.CreateTestStudent(id: 2)
            }.AsQueryable().BuildMock();

            MockPermissionService.Setup(p => p.GetCurrentUserRoleAsync()).ReturnsAsync(Roles.Director);
            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync("director-user-id");
            MockPermissionService.Setup(p => p.HasPermissionAsync(Permissions.ViewStudents)).ReturnsAsync(true);
            _mockStudentRepo.Setup(r => r.GetQueryable()).Returns(students);

            // Act
            var result = await _service.GetAccessibleStudentsAsync(1, 10);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAccessibleStudentsAsync_WithoutAuthentication_ReturnsFailure()
        {
            // Arrange
            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync((string)null);

            // Act
            var result = await _service.GetAccessibleStudentsAsync(1, 10);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("User not authenticated");
        }

        [Fact]
        public async Task GetAccessibleStudentsAsync_AsBandStaffWithoutPermission_ReturnsForbidden()
        {
            // Arrange
            MockPermissionService.Setup(p => p.GetCurrentUserRoleAsync()).ReturnsAsync(Roles.BandStaff);
            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync("bandstaff-user-id");
            MockPermissionService.Setup(p => p.HasPermissionAsync(Permissions.ViewStudents)).ReturnsAsync(false);

            // Act
            var result = await _service.GetAccessibleStudentsAsync(1, 10);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("No permission");
        }

        #endregion

        #region GetStudentDashboardAsync Tests

        [Fact]
        public async Task GetStudentDashboardAsync_WithValidStudent_ReturnsDashboard()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1, applicationUserId: "student-user-id");
            var offer1 = TestDataBuilder.CreateTestScholarshipOffer(id: 1, studentId: 1, status: ScholarshipStatus.Sent);
            var offer2 = TestDataBuilder.CreateTestScholarshipOffer(id: 2, studentId: 1, status: ScholarshipStatus.Accepted);
            student.ScholarshipOffers = new List<ScholarshipOffer> { offer1, offer2 };
            
            var contactRequest = TestDataBuilder.CreateTestContactRequest(id: 1, studentId: 1, status: "Pending");
            var students = new List<Student> { student }.AsQueryable().BuildMock();
            var contactRequests = new List<ContactRequest> { contactRequest }.AsQueryable().BuildMock();
            var notifications = new List<Notification>().AsQueryable().BuildMock();

            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync("student-user-id");
            _mockStudentRepo.Setup(r => r.GetQueryable()).Returns(students);
            _mockContactRequestRepo.Setup(r => r.GetQueryable()).Returns(contactRequests);
            _mockNotificationRepo.Setup(r => r.GetQueryable()).Returns(notifications);

            // Act
            var result = await _service.GetStudentDashboardAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.StudentId.Should().Be(1);
            result.Data.FirstName.Should().Be("John");
            result.Data.ActiveOffers.Should().Be(2);
            result.Data.PendingContactRequests.Should().Be(1);
        }

        [Fact]
        public async Task GetStudentDashboardAsync_WithoutAuthentication_ReturnsFailure()
        {
            // Arrange
            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync((string)null);

            // Act
            var result = await _service.GetStudentDashboardAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("User not authenticated");
        }

        [Fact]
        public async Task GetStudentDashboardAsync_WithNonExistentStudent_ReturnsFailure()
        {
            // Arrange
            var students = new List<Student>().AsQueryable().BuildMock();
            
            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync("unknown-user-id");
            _mockStudentRepo.Setup(r => r.GetQueryable()).Returns(students);

            // Act
            var result = await _service.GetStudentDashboardAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Student profile not found");
        }

        [Fact]
        public async Task GetStudentDashboardAsync_IncludesRecentNotifications()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1, applicationUserId: "student-user-id");
            var notification1 = new Notification 
            { 
                Id = 1, 
                UserId = "student-user-id", 
                Title = "New Offer", 
                Message = "You have a new offer",
                Type = "Offer",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            
            var students = new List<Student> { student }.AsQueryable().BuildMock();
            var contactRequests = new List<ContactRequest>().AsQueryable().BuildMock();
            var notifications = new List<Notification> { notification1 }.AsQueryable().BuildMock();

            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync("student-user-id");
            _mockStudentRepo.Setup(r => r.GetQueryable()).Returns(students);
            _mockContactRequestRepo.Setup(r => r.GetQueryable()).Returns(contactRequests);
            _mockNotificationRepo.Setup(r => r.GetQueryable()).Returns(notifications);

            // Act
            var result = await _service.GetStudentDashboardAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.RecentNotifications.Should().HaveCount(1);
            result.Data.RecentNotifications.First().Title.Should().Be("New Offer");
        }

        [Fact]
        public async Task GetStudentDashboardAsync_ExcludesDeclinedAndRescindedOffers()
        {
            // Arrange
            var student = TestDataBuilder.CreateTestStudent(id: 1, applicationUserId: "student-user-id");
            var offer1 = TestDataBuilder.CreateTestScholarshipOffer(id: 1, studentId: 1, status: ScholarshipStatus.Sent);
            var offer2 = TestDataBuilder.CreateTestScholarshipOffer(id: 2, studentId: 1, status: ScholarshipStatus.Declined);
            var offer3 = TestDataBuilder.CreateTestScholarshipOffer(id: 3, studentId: 1, status: ScholarshipStatus.Rescinded);
            var offer4 = TestDataBuilder.CreateTestScholarshipOffer(id: 4, studentId: 1, status: ScholarshipStatus.Draft);
            student.ScholarshipOffers = new List<ScholarshipOffer> { offer1, offer2, offer3, offer4 };
            
            var students = new List<Student> { student }.AsQueryable().BuildMock();
            var contactRequests = new List<ContactRequest>().AsQueryable().BuildMock();
            var notifications = new List<Notification>().AsQueryable().BuildMock();

            MockPermissionService.Setup(p => p.GetCurrentUserIdAsync()).ReturnsAsync("student-user-id");
            _mockStudentRepo.Setup(r => r.GetQueryable()).Returns(students);
            _mockContactRequestRepo.Setup(r => r.GetQueryable()).Returns(contactRequests);
            _mockNotificationRepo.Setup(r => r.GetQueryable()).Returns(notifications);

            // Act
            var result = await _service.GetStudentDashboardAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.ActiveOffers.Should().Be(1); // Only the Sent offer
        }

        #endregion
    }
}
