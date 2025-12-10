using Hangfire;
using Microsoft.EntityFrameworkCore;
using Podium.Application.Interfaces; // Assuming IVideoService exists
using Podium.Infrastructure.Data;

namespace Podium.API.Jobs
{
    public class ProcessTranscodingQueueJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IVideoService _videoService; // Use your existing video service logic

        public ProcessTranscodingQueueJob(ApplicationDbContext context, IVideoService videoService)
        {
            _context = context;
            _videoService = videoService;
        }

        [AutomaticRetry(Attempts = 3)] // Built-in Hangfire retry logic
        public async Task ExecuteAsync()
        {
            var pendingVideos = await _context.Videos
                .Where(v => v.TranscodingStatus == "Pending")
                .OrderBy(v => v.UploadedDate)
                .Take(5) // Process in batches
                .ToListAsync();

            foreach (var video in pendingVideos)
            {
                try
                {
                    video.TranscodingStatus = "Processing";
                    await _context.SaveChangesAsync();

                    // Call your actual transcoding logic here
                    // await _videoService.TranscodeAsync(video.VideoId); 

                    video.TranscodingStatus = "Completed";
                    video.CompletedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    video.TranscodingStatus = "Failed";
                    video.TranscodingError = ex.Message;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
