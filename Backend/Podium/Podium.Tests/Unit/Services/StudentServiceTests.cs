using Xunit;
using Moq;
using FluentAssertions;
using Podium.Application.Services;
using Podium.Core.Interfaces;
using Podium.Core.Entities;

namespace Podium.Tests.Unit.Services
{
    /// <summary>
    /// Unit tests for StudentService
    /// These tests verify business logic without hitting the database
    /// </summary>
    public class StudentServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IStudentRepository> _mockStudentRepo;
        private readonly StudentService _service;

        public StudentServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockStudentRepo = new Mock<IStudentRepository>();
            
            // Setup the repository mock on UnitOfWork
            _mockUnitOfWork.Setup(u => u.Students).Returns(_mockStudentRepo.Object);
            
            _service = new StudentService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetStudentById_ReturnsStudent_WhenExists()
        {
            // Arrange
            var studentId = 1;
            var expectedStudent = new Student
            {
                Id = studentId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@test.com",
                Instrument = "Trumpet",
                School = "Test University",
                GraduationYear = 2025
            };

            _mockStudentRepo
                .Setup(r => r.GetByIdAsync(studentId))
                .ReturnsAsync(expectedStudent);

            // Act
            var result = await _service.GetStudentByIdAsync(studentId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(studentId);
            result.FirstName.Should().Be("John");
            result.LastName.Should().Be("Doe");
            result.Instrument.Should().Be("Trumpet");
            
            // Verify the repository was called exactly once
            _mockStudentRepo.Verify(r => r.GetByIdAsync(studentId), Times.Once);
        }

        [Fact]
        public async Task GetStudentById_ReturnsNull_WhenNotExists()
        {
            // Arrange
            var nonExistentId = 999;
            _mockStudentRepo
                .Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Student?)null);

            // Act
            var result = await _service.GetStudentByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
            _mockStudentRepo.Verify(r => r.GetByIdAsync(nonExistentId), Times.Once);
        }

        [Fact]
        public async Task SearchStudents_FiltersBy_Instrument()
        {
            // Arrange
            var instrument = "Trumpet";
            var students = new List<Student>
            {
                new Student { Id = 1, Instrument = "Trumpet", FirstName = "John" },
                new Student { Id = 2, Instrument = "Trumpet", FirstName = "Jane" }
            };

            _mockStudentRepo
                .Setup(r => r.SearchAsync(
                    It.Is<string?>(i => i == instrument),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync(students);

            // Act
            var result = await _service.SearchStudentsAsync(
                instrument: instrument,
                school: null,
                graduationYear: null,
                page: 1,
                pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().OnlyContain(s => s.Instrument == "Trumpet");
        }

        [Fact]
        public async Task SearchStudents_CombinesMultipleFilters()
        {
            // Arrange
            var instrument = "Clarinet";
            var school = "Berkeley";
            var graduationYear = 2025;

            var filteredStudents = new List<Student>
            {
                new Student 
                { 
                    Id = 1, 
                    Instrument = instrument, 
                    School = school,
                    GraduationYear = graduationYear,
                    FirstName = "Alice"
                }
            };

            _mockStudentRepo
                .Setup(r => r.SearchAsync(
                    instrument,
                    school,
                    graduationYear,
                    1,
                    10))
                .ReturnsAsync(filteredStudents);

            // Act
            var result = await _service.SearchStudentsAsync(
                instrument: instrument,
                school: school,
                graduationYear: graduationYear,
                page: 1,
                pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Instrument.Should().Be(instrument);
            result.First().School.Should().Be(school);
            result.First().GraduationYear.Should().Be(graduationYear);
        }

        [Fact]
        public async Task UpdateStudent_UpdatesSuccessfully_WhenValidData()
        {
            // Arrange
            var studentId = 1;
            var existingStudent = new Student
            {
                Id = studentId,
                FirstName = "John",
                LastName = "Doe",
                Instrument = "Trumpet"
            };

            var updateData = new Student
            {
                Id = studentId,
                FirstName = "John",
                LastName = "Doe",
                Instrument = "Trombone" // Changed
            };

            _mockStudentRepo
                .Setup(r => r.GetByIdAsync(studentId))
                .ReturnsAsync(existingStudent);

            _mockStudentRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Student>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _service.UpdateStudentAsync(updateData);

            // Assert
            _mockStudentRepo.Verify(r => r.UpdateAsync(It.Is<Student>(s => 
                s.Id == studentId && 
                s.Instrument == "Trombone")), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Theory]
        [InlineData(1, 10)] // First page
        [InlineData(2, 10)] // Second page
        [InlineData(1, 20)] // Larger page size
        public async Task SearchStudents_HandlesPagination_Correctly(int page, int pageSize)
        {
            // Arrange
            var students = new List<Student>();
            for (int i = 0; i < pageSize; i++)
            {
                students.Add(new Student 
                { 
                    Id = i + 1, 
                    FirstName = $"Student{i}" 
                });
            }

            _mockStudentRepo
                .Setup(r => r.SearchAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    page,
                    pageSize))
                .ReturnsAsync(students);

            // Act
            var result = await _service.SearchStudentsAsync(
                instrument: null,
                school: null,
                graduationYear: null,
                page: page,
                pageSize: pageSize);

            // Assert
            result.Should().HaveCount(pageSize);
            _mockStudentRepo.Verify(r => r.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                page,
                pageSize), Times.Once);
        }
    }
}
