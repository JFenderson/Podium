using Podium.Core.Entities;

namespace Podium.Tests.Builders
{
    /// <summary>
    /// Builder pattern for creating Student test entities
    /// Provides a fluent API for constructing test data
    /// </summary>
    public class StudentBuilder
    {
        private Student _student = new Student
        {
            FirstName = "Test",
            LastName = "Student",
            Email = "test.student@example.com",
            Instrument = "Trumpet",
            School = "Test University",
            GraduationYear = 2025,
            Bio = "Test bio",
            PhoneNumber = "+1234567890",
            State = "CA",
            City = "Los Angeles",
            IsProfilePublic = true,
            CreatedAt = DateTime.UtcNow
        };

        public StudentBuilder WithId(int id)
        {
            _student.Id = id;
            return this;
        }

        public StudentBuilder WithName(string firstName, string lastName)
        {
            _student.FirstName = firstName;
            _student.LastName = lastName;
            return this;
        }

        public StudentBuilder WithEmail(string email)
        {
            _student.Email = email;
            return this;
        }

        public StudentBuilder WithInstrument(string instrument)
        {
            _student.Instrument = instrument;
            return this;
        }

        public StudentBuilder WithSchool(string school)
        {
            _student.School = school;
            return this;
        }

        public StudentBuilder WithGraduationYear(int year)
        {
            _student.GraduationYear = year;
            return this;
        }

        public StudentBuilder WithLocation(string city, string state)
        {
            _student.City = city;
            _student.State = state;
            return this;
        }

        public StudentBuilder WithBio(string bio)
        {
            _student.Bio = bio;
            return this;
        }

        public StudentBuilder WithPhoneNumber(string phoneNumber)
        {
            _student.PhoneNumber = phoneNumber;
            return this;
        }

        public StudentBuilder WithProfileVisibility(bool isPublic)
        {
            _student.IsProfilePublic = isPublic;
            return this;
        }

        public StudentBuilder WithCreatedAt(DateTime createdAt)
        {
            _student.CreatedAt = createdAt;
            return this;
        }

        public StudentBuilder WithUserId(string userId)
        {
            _student.UserId = userId;
            return this;
        }

        /// <summary>
        /// Builds and returns the Student instance
        /// </summary>
        public Student Build()
        {
            return _student;
        }

        /// <summary>
        /// Creates a new builder with default values
        /// </summary>
        public static StudentBuilder Default()
        {
            return new StudentBuilder();
        }

        /// <summary>
        /// Creates a builder with a specific instrument
        /// </summary>
        public static StudentBuilder ForInstrument(string instrument)
        {
            return new StudentBuilder().WithInstrument(instrument);
        }

        /// <summary>
        /// Creates a builder for a graduating senior
        /// </summary>
        public static StudentBuilder Senior(int graduationYear)
        {
            return new StudentBuilder()
                .WithGraduationYear(graduationYear);
        }
    }
}
