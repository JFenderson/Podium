using Google.Api.Ads.AdWords.v201809;
using Podium.Application.DTOs.Video;
using Podium.Application.Services;
using Podium.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Video = Podium.Core.Entities.Video;

namespace Podium.Application.Interfaces
{
    public interface IVideoService
    {
        Task<List<MyVideoListItem>> GetMyVideosAsync(int studentId);
        Task<VideoResponse> GetVideoDetailsAsync(int videoId, int? currentUserId, string userRole);
        Task<(bool success, string message)> ValidateVideoUploadAsync(int studentId, long fileSizeBytes);
        (bool success, string message) ValidateVideoFileType(string fileName, string contentType);
        Task<Video> CreateVideoAsync(int studentId, CreateVideoRequest request);
        Task<Video> UpdateVideoAsync(int videoId, int studentId, UpdateVideoRequest request);
        Task<bool> SoftDeleteVideoAsync(int videoId, int studentId);
        Task<bool> IncrementViewCountAsync(int videoId);
        Task<List<VideoRatingResponse>> GetVideoRatingsAsync(int videoId);
        Task<VideoRatingResponse> RateVideoAsync(int videoId, int recruiterId, RateVideoRequest request);
        Task<VideoRatingResponse> UpdateRatingAsync(int videoId, int recruiterId, RateVideoRequest request);
        Task<bool> UpdateTranscodingStatusAsync(string uploadId, TranscodingWebhookRequest request);
        Task<ServiceResult<PagedResult<VideoThumbnailDto>>> GetStudentVideoThumbnailsAsync(int studentId, int page, int pageSize);
    }
}
