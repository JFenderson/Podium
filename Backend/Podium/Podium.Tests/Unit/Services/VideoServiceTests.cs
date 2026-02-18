using Xunit;
using Moq;
using FluentAssertions;
using Podium.Application.Services;
using Podium.Application.DTOs.Video;
using Podium.Core.Entities;
using Podium.Core.Constants;
using Podium.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MockQueryable.Moq;

namespace Podium.Tests.Unit.Services
{
    public class VideoServiceTests : TestBase
    {
        private readonly VideoService _service;
        private readonly Mock<Core.Interfaces.IRepository<Video>> _mockVideoRepo;
        private readonly Mock<Core.Interfaces.IRepository<VideoRating>> _mockVideoRatingRepo;

        public VideoServiceTests()
        {
            _mockVideoRepo = new Mock<Core.Interfaces.IRepository<Video>>();
            _mockVideoRatingRepo = new Mock<Core.Interfaces.IRepository<VideoRating>>();

            MockUnitOfWork.Setup(u => u.Videos).Returns(_mockVideoRepo.Object);
            MockUnitOfWork.Setup(u => u.VideoRatings).Returns(_mockVideoRatingRepo.Object);

            _service = new VideoService(
                MockUnitOfWork.Object,
                MockVideoStorageService.Object,
                MockLogger<VideoService>().Object,
                MockConfiguration.Object
            );
        }

        #region GetMyVideosAsync Tests

        [Fact]
        public async Task GetMyVideosAsync_WithValidStudentId_ReturnsVideoList()
        {
            // Arrange
            var studentId = 1;
            var videos = new List<Video>
            {
                TestDataBuilder.CreateTestVideo(id: 1, studentId: studentId, title: "Video 1"),
                TestDataBuilder.CreateTestVideo(id: 2, studentId: studentId, title: "Video 2")
            };

            _mockVideoRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Video, bool>>>()))
                .ReturnsAsync(videos);

            // Act
            var result = await _service.GetMyVideosAsync(studentId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Title.Should().Be("Video 1");
            result[1].Title.Should().Be("Video 2");
            result.All(v => v.VideoId > 0).Should().BeTrue();
        }

        [Fact]
        public async Task GetMyVideosAsync_WithNoVideos_ReturnsEmptyList()
        {
            // Arrange
            var studentId = 1;
            _mockVideoRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Video, bool>>>()))
                .ReturnsAsync(new List<Video>());

            // Act
            var result = await _service.GetMyVideosAsync(studentId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetVideoDetailsAsync Tests

        [Fact]
        public async Task GetVideoDetailsAsync_WithValidVideoAndStudent_ReturnsVideoDetails()
        {
            // Arrange
            var studentId = 1;
            var video = TestDataBuilder.CreateTestVideo(id: 1, studentId: studentId);
            var videoUrl = "https://signed-url.com/video.mp4";

            _mockVideoRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(video);
            MockVideoStorageService.Setup(s => s.GetVideoUrlAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(videoUrl);

            // Act
            var result = await _service.GetVideoDetailsAsync(1, studentId, Roles.Student);

            // Assert
            result.Should().NotBeNull();
            result.VideoId.Should().Be(1);
            result.Title.Should().Be(video.Title);
            result.VideoUrl.Should().Be(videoUrl);
        }

        [Fact]
        public async Task GetVideoDetailsAsync_WithNonExistentVideo_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockVideoRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Video)null);

            // Act
            Func<Task> act = async () => await _service.GetVideoDetailsAsync(999, 1, Roles.Student);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Video not found");
        }

        [Fact]
        public async Task GetVideoDetailsAsync_StudentAccessingOtherStudentVideo_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var video = TestDataBuilder.CreateTestVideo(id: 1, studentId: 1);
            _mockVideoRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(video);

            // Act
            Func<Task> act = async () => await _service.GetVideoDetailsAsync(1, 2, Roles.Student);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Cannot view this video.");
        }

        [Fact]
        public async Task GetVideoDetailsAsync_StaffAccessingAnyVideo_ReturnsVideoDetails()
        {
            // Arrange
            var video = TestDataBuilder.CreateTestVideo(id: 1, studentId: 1);
            var videoUrl = "https://signed-url.com/video.mp4";

            _mockVideoRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(video);
            MockVideoStorageService.Setup(s => s.GetVideoUrlAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(videoUrl);

            // Act
            var result = await _service.GetVideoDetailsAsync(1, 99, Roles.BandStaff);

            // Assert
            result.Should().NotBeNull();
            result.VideoId.Should().Be(1);
            MockVideoStorageService.Verify(s => s.GetVideoUrlAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task GetVideoDetailsAsync_WithUploadingVideo_ReturnsEmptyVideoUrl()
        {
            // Arrange
            var video = TestDataBuilder.CreateTestVideo(id: 1, studentId: 1);
            video.Status = VideoStatus.Uploading;

            _mockVideoRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(video);

            // Act
            var result = await _service.GetVideoDetailsAsync(1, 1, Roles.Student);

            // Assert
            result.Should().NotBeNull();
            result.VideoUrl.Should().BeEmpty();
            MockVideoStorageService.Verify(s => s.GetVideoUrlAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region ValidateVideoUploadAsync Tests

        [Fact]
        public async Task ValidateVideoUploadAsync_WithinSizeLimit_ReturnsSuccess()
        {
            // Arrange
            var studentId = 1;
            var fileSizeBytes = 100 * 1024 * 1024; // 100MB
            
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x.Value).Returns("500");
            MockConfiguration.Setup(x => x.GetSection("Video:MaxFileSizeMB")).Returns(configSection.Object);

            var service = new VideoService(
                MockUnitOfWork.Object,
                MockVideoStorageService.Object,
                MockLogger<VideoService>().Object,
                MockConfiguration.Object
            );

            // Act
            var result = await service.ValidateVideoUploadAsync(studentId, fileSizeBytes);

            // Assert
            result.success.Should().BeTrue();
            result.message.Should().Be("Upload approved");
        }

        [Fact]
        public async Task ValidateVideoUploadAsync_ExceedingSizeLimit_ReturnsFailure()
        {
            // Arrange
            var studentId = 1;
            var fileSizeBytes = 600L * 1024 * 1024; // 600MB
            
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x.Value).Returns("500");
            MockConfiguration.Setup(x => x.GetSection("Video:MaxFileSizeMB")).Returns(configSection.Object);

            var service = new VideoService(
                MockUnitOfWork.Object,
                MockVideoStorageService.Object,
                MockLogger<VideoService>().Object,
                MockConfiguration.Object
            );

            // Act
            var result = await service.ValidateVideoUploadAsync(studentId, fileSizeBytes);

            // Assert
            result.success.Should().BeFalse();
            result.message.Should().Contain("exceeds");
            result.message.Should().Contain("500MB");
        }

        [Fact]
        public async Task ValidateVideoUploadAsync_WithMissingConfig_UsesDefaultLimit()
        {
            // Arrange
            var studentId = 1;
            var fileSizeBytes = 400L * 1024 * 1024; // 400MB
            
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x.Value).Returns("0");
            MockConfiguration.Setup(x => x.GetSection("Video:MaxFileSizeMB")).Returns(configSection.Object);

            var service = new VideoService(
                MockUnitOfWork.Object,
                MockVideoStorageService.Object,
                MockLogger<VideoService>().Object,
                MockConfiguration.Object
            );

            // Act
            var result = await service.ValidateVideoUploadAsync(studentId, fileSizeBytes);

            // Assert
            result.success.Should().BeTrue(); // Should use default 500MB
        }

        #endregion

        #region CreateVideoAsync Tests

        [Fact]
        public async Task CreateVideoAsync_WithValidRequest_CreatesVideoSuccessfully()
        {
            // Arrange
            var studentId = 1;
            var request = new CreateVideoRequest
            {
                Title = "Test Video",
                Description = "Test Description",
                Instrument = "Trumpet",
                FileName = "test-video.mp4",
                IsPublic = true
            };

            _mockVideoRepo.Setup(r => r.AddAsync(It.IsAny<Video>())).Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.CreateVideoAsync(studentId, request);

            // Assert
            result.Should().NotBeNull();
            result.StudentId.Should().Be(studentId);
            result.Title.Should().Be("Test Video");
            result.Description.Should().Be("Test Description");
            result.Instrument.Should().Be("Trumpet");
            result.Status.Should().Be(VideoStatus.Uploading);
            result.ViewCount.Should().Be(0);
            result.IsReviewed.Should().BeFalse();
            result.Url.Should().Contain(studentId.ToString());
            result.Url.Should().Contain("test-video.mp4");

            _mockVideoRepo.Verify(r => r.AddAsync(It.IsAny<Video>()), Times.Once);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateVideoAsync_GeneratesUniqueFileName()
        {
            // Arrange
            var studentId = 1;
            var request = new CreateVideoRequest
            {
                Title = "Test Video",
                Instrument = "Trumpet",
                FileName = "test.mp4",
                IsPublic = true
            };

            Video capturedVideo = null;
            _mockVideoRepo.Setup(r => r.AddAsync(It.IsAny<Video>()))
                .Callback<Video>(v => capturedVideo = v)
                .Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _service.CreateVideoAsync(studentId, request);

            // Assert
            capturedVideo.Should().NotBeNull();
            capturedVideo.Url.Should().StartWith($"{studentId}/");
            capturedVideo.Url.Should().EndWith("_test.mp4");
            capturedVideo.Url.Should().MatchRegex(@"\d+/[a-f0-9\-]+_test\.mp4");
        }

        #endregion

        #region RateVideoAsync Tests

        [Fact]
        public async Task RateVideoAsync_WithNewRating_CreatesRatingSuccessfully()
        {
            // Arrange
            var videoId = 1;
            var recruiterId = 10;
            var video = TestDataBuilder.CreateTestVideo(id: videoId);
            video.IsReviewed = false;

            var request = new RateVideoRequest
            {
                Rating = 5,
                Comment = "Excellent performance!"
            };

            _mockVideoRepo.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync(video);
            _mockVideoRatingRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<VideoRating, bool>>>()))
                .ReturnsAsync((VideoRating)null);
            _mockVideoRatingRepo.Setup(r => r.AddAsync(It.IsAny<VideoRating>())).Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.RateVideoAsync(videoId, recruiterId, request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Rating submitted");

            _mockVideoRatingRepo.Verify(r => r.AddAsync(It.Is<VideoRating>(vr =>
                vr.VideoId == videoId &&
                vr.BandStaffId == recruiterId &&
                vr.Rating == 5 &&
                vr.Comment == "Excellent performance!"
            )), Times.Once);

            _mockVideoRepo.Verify(r => r.Update(It.Is<Video>(v => v.IsReviewed == true)), Times.Once);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RateVideoAsync_WithExistingRating_UpdatesRating()
        {
            // Arrange
            var videoId = 1;
            var recruiterId = 10;
            var video = TestDataBuilder.CreateTestVideo(id: videoId);
            video.IsReviewed = true;

            var existingRating = new VideoRating
            {
                Id = 1,
                VideoId = videoId,
                BandStaffId = recruiterId,
                Rating = 3,
                Comment = "Good",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var request = new RateVideoRequest
            {
                Rating = 5,
                Comment = "Updated to excellent!"
            };

            _mockVideoRepo.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync(video);
            _mockVideoRatingRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<VideoRating, bool>>>()))
                .ReturnsAsync(existingRating);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.RateVideoAsync(videoId, recruiterId, request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            existingRating.Rating.Should().Be(5);
            existingRating.Comment.Should().Be("Updated to excellent!");

            _mockVideoRatingRepo.Verify(r => r.Update(existingRating), Times.Once);
            _mockVideoRatingRepo.Verify(r => r.AddAsync(It.IsAny<VideoRating>()), Times.Never);
        }

        [Fact]
        public async Task RateVideoAsync_WithNonExistentVideo_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockVideoRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Video)null);

            var request = new RateVideoRequest { Rating = 5, Comment = "Great" };

            // Act
            Func<Task> act = async () => await _service.RateVideoAsync(999, 1, request);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Video not found");
        }

        [Fact]
        public async Task RateVideoAsync_MarksVideoAsReviewedOnFirstRating()
        {
            // Arrange
            var videoId = 1;
            var video = TestDataBuilder.CreateTestVideo(id: videoId);
            video.IsReviewed = false;

            var request = new RateVideoRequest { Rating = 4, Comment = "Nice" };

            _mockVideoRepo.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync(video);
            _mockVideoRatingRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<VideoRating, bool>>>()))
                .ReturnsAsync((VideoRating)null);
            _mockVideoRatingRepo.Setup(r => r.AddAsync(It.IsAny<VideoRating>())).Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _service.RateVideoAsync(videoId, 1, request);

            // Assert
            video.IsReviewed.Should().BeTrue();
            _mockVideoRepo.Verify(r => r.Update(It.Is<Video>(v => v.IsReviewed)), Times.Once);
        }

        #endregion

        #region GetVideoRatingsAsync Tests

        [Fact]
        public async Task GetVideoRatingsAsync_WithValidVideoId_ReturnsRatings()
        {
            // Arrange
            var videoId = 1;
            var video = TestDataBuilder.CreateTestVideo(id: videoId);
            var bandStaff = TestDataBuilder.CreateTestBandStaff(id: 1, firstName: "John", lastName: "Recruiter");

            var ratings = new List<VideoRating>
            {
                new VideoRating
                {
                    Id = 1,
                    VideoId = videoId,
                    BandStaffId = 1,
                    Rating = 5,
                    Comment = "Excellent!",
                    CreatedAt = DateTime.UtcNow,
                    BandStaff = bandStaff
                }
            };

            _mockVideoRepo.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync(video);

            var ratingsQueryable = ratings.AsQueryable().BuildMock();
            _mockVideoRatingRepo.Setup(r => r.GetQueryable()).Returns(ratingsQueryable);

            // Act
            var result = await _service.GetVideoRatingsAsync(videoId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].RatingId.Should().Be(1);
            result[0].Rating.Should().Be(5);
            result[0].Comment.Should().Be("Excellent!");
            result[0].BandStaffName.Should().Be("John Recruiter");
            result[0].Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetVideoRatingsAsync_WithNonExistentVideo_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockVideoRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Video)null);

            // Act
            Func<Task> act = async () => await _service.GetVideoRatingsAsync(999);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Video not found");
        }

        [Fact]
        public async Task GetVideoRatingsAsync_WithNoRatings_ReturnsEmptyList()
        {
            // Arrange
            var videoId = 1;
            var video = TestDataBuilder.CreateTestVideo(id: videoId);

            _mockVideoRepo.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync(video);

            var emptyRatings = new List<VideoRating>().AsQueryable().BuildMock();
            _mockVideoRatingRepo.Setup(r => r.GetQueryable()).Returns(emptyRatings);

            // Act
            var result = await _service.GetVideoRatingsAsync(videoId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetVideoRatingsAsync_ReturnsRatingsOrderedByCreatedAtDescending()
        {
            // Arrange
            var videoId = 1;
            var video = TestDataBuilder.CreateTestVideo(id: videoId);
            var bandStaff = TestDataBuilder.CreateTestBandStaff();

            var ratings = new List<VideoRating>
            {
                new VideoRating
                {
                    Id = 1,
                    VideoId = videoId,
                    BandStaffId = 1,
                    Rating = 4,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    BandStaff = bandStaff
                },
                new VideoRating
                {
                    Id = 2,
                    VideoId = videoId,
                    BandStaffId = 1,
                    Rating = 5,
                    CreatedAt = DateTime.UtcNow,
                    BandStaff = bandStaff
                }
            };

            _mockVideoRepo.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync(video);

            var ratingsQueryable = ratings.AsQueryable().BuildMock();
            _mockVideoRatingRepo.Setup(r => r.GetQueryable()).Returns(ratingsQueryable);

            // Act
            var result = await _service.GetVideoRatingsAsync(videoId);

            // Assert
            result.Should().HaveCount(2);
            result[0].RatingId.Should().Be(2); // Most recent first
            result[1].RatingId.Should().Be(1);
        }

        #endregion

        #region IncrementViewCountAsync Tests

        [Fact]
        public async Task IncrementViewCountAsync_WithValidVideoId_IncrementsViewCount()
        {
            // Arrange
            var videoId = 1;
            var video = TestDataBuilder.CreateTestVideo(id: videoId);
            video.ViewCount = 5;

            _mockVideoRepo.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync(video);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.IncrementViewCountAsync(videoId);

            // Assert
            result.Should().BeTrue();
            video.ViewCount.Should().Be(6);
            _mockVideoRepo.Verify(r => r.Update(video), Times.Once);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task IncrementViewCountAsync_WithNonExistentVideo_ReturnsFalse()
        {
            // Arrange
            _mockVideoRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Video)null);

            // Act
            var result = await _service.IncrementViewCountAsync(999);

            // Assert
            result.Should().BeFalse();
            _mockVideoRepo.Verify(r => r.Update(It.IsAny<Video>()), Times.Never);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task IncrementViewCountAsync_WithZeroViewCount_IncrementsToOne()
        {
            // Arrange
            var videoId = 1;
            var video = TestDataBuilder.CreateTestVideo(id: videoId);
            video.ViewCount = 0;

            _mockVideoRepo.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync(video);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.IncrementViewCountAsync(videoId);

            // Assert
            result.Should().BeTrue();
            video.ViewCount.Should().Be(1);
        }

        #endregion
    }
}
