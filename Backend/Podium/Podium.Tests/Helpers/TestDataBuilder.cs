using Podium.Core.Entities;
using Podium.Core.Constants;
using System;
using System.Collections.Generic;

namespace Podium.Tests.Helpers
{
    /// <summary>
    /// Builder class for creating test data entities
    /// </summary>
    public static class TestDataBuilder
    {
        public static ApplicationUser CreateTestApplicationUser(
            string id = "test-user-id",
            string email = "test@example.com",
            string firstName = "Test",
            string lastName = "User",
            bool emailConfirmed = true,
            bool isActive = true)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = emailConfirmed,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Student CreateTestStudent(
            int id = 1,
            string firstName = "John",
            string lastName = "Doe",
            string email = "student@test.edu",
            string primaryInstrument = "Trumpet",
            int graduationYear = 2025,
            string? applicationUserId = null)
        {
            return new Student
            {
                Id = id,
                ApplicationUserId = applicationUserId ?? "student-user-id",
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PrimaryInstrument = primaryInstrument,
                GraduationYear = graduationYear,
                HighSchool = "Test High School",
                State = "CA",
                PhoneNumber = "555-0100",
                Bio = "Test student bio",
                GPA = 3.5m,
                SkillLevel = "Intermediate",
                IsDeleted = false,
                Videos = new List<Video>(),
                StudentRatings = new List<StudentRating>(),
                Guardians = new List<Guardian>(),
                StudentInterests = new List<StudentInterest>()
            };
        }

        public static Guardian CreateTestGuardian(
            int id = 1,
            string firstName = "Jane",
            string lastName = "Parent",
            string email = "parent@test.com",
            string? applicationUserId = null)
        {
            return new Guardian
            {
                Id = id,
                ApplicationUserId = applicationUserId ?? "guardian-user-id",
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = "555-0200",
                Students = new List<Student>()
            };
        }

        public static BandStaff CreateTestBandStaff(
            int id = 1,
            int bandId = 1,
            string firstName = "Mike",
            string lastName = "Recruiter",
            string role = "Recruiter",
            string? applicationUserId = null)
        {
            return new BandStaff
            {
                Id = id,
                BandId = bandId,
                ApplicationUserId = applicationUserId ?? "bandstaff-user-id",
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                IsActive = true,
                CreatedBy = applicationUserId ?? "director-user-id"
            };
        }

        public static Band CreateTestBand(
            int id = 1,
            string name = "Test University Band",
            string university = "Test University",
            string? director = null,
            decimal scholarshipBudget = 100000)
        {
            return new Band
            {
                Id = id,
                BandName = name,
                UniversityName = university,
                DirectorApplicationUserId = director,
                ScholarshipBudget = scholarshipBudget,
                State = "CA",
                City = "Los Angeles",
                IsActive = true,
                IsDeleted = false,
                Staff = new List<BandStaff>(),
                Offers = new List<ScholarshipOffer>()
            };
        }

        public static Video CreateTestVideo(
            int id = 1,
            int studentId = 1,
            string title = "Test Performance",
            string instrument = "Trumpet",
            string url = "https://test.com/video.mp4")
        {
            return new Video
            {
                Id = id,
                StudentId = studentId,
                Title = title,
                Description = "Test video description",
                Instrument = instrument,
                Url = url,
                ThumbnailUrl = "https://test.com/thumb.jpg",
                Status = VideoStatus.Ready,
                TranscodingStatus = "Completed",
                TranscodingError = "",
                ViewCount = 0,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                Ratings = new List<VideoRating>()
            };
        }

        public static ScholarshipOffer CreateTestScholarshipOffer(
            int id = 1,
            int studentId = 1,
            int bandId = 1,
            decimal amount = 5000,
            ScholarshipStatus? status = null,
            string offerType = "Partial",
            bool requiresGuardianApproval = false)
        {
            return new ScholarshipOffer
            {
                Id = id,
                StudentId = studentId,
                BandId = bandId,
                ScholarshipAmount = amount,
                Status = status ?? ScholarshipStatus.Sent,
                OfferType = offerType,
                RequiresGuardianApproval = requiresGuardianApproval,
                Description = "Test scholarship offer",
                CreatedByUserId = "director-user-id",
                CreatedAt = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                IsDeleted = false
            };
        }

        public static BandBudget CreateTestBandBudget(
            int id = 1,
            int bandId = 1,
            int fiscalYear = 2025,
            decimal totalBudget = 100000,
            decimal allocatedAmount = 20000,
            decimal remainingAmount = 80000)
        {
            return new BandBudget
            {
                Id = id,
                BandId = bandId,
                FiscalYear = fiscalYear,
                TotalBudget = totalBudget,
                AllocatedAmount = allocatedAmount,
                RemainingAmount = remainingAmount,
                CreatedBy = "director-user-id",
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[] { 0, 0, 0, 1 }
            };
        }

        public static ContactRequest CreateTestContactRequest(
            int id = 1,
            int studentId = 1,
            int bandStaffId = 1,
            string status = "Pending",
            bool requiresGuardianApproval = true)
        {
            return new ContactRequest
            {
                Id = id,
                StudentId = studentId,
                BandStaffId = bandStaffId,
                BandId = 1,
                Status = status,
                Purpose = "Test contact request",
                CreatedBy = "bandstaff-user-id",
                CreatedAt = DateTime.UtcNow
            };
        }

        public static RefreshToken CreateTestRefreshToken(
            string token = "test-refresh-token",
            string userId = "test-user-id",
            bool isRevoked = false,
            DateTime? expiresAt = null)
        {
            return new RefreshToken
            {
                Token = token,
                ApplicationUserId = userId,
                IsRevoked = isRevoked,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7)
            };
        }
    }
}
