using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Interfaces
{
    public interface IVideoStorageService
    {
        /// <summary>
        /// Generates a secure URL that the frontend can use to upload the file directly.
        /// </summary>
        /// <param name="fileName">The relative path/name of the file.</param>
        /// <param name="contentType">The MIME type (e.g. video/mp4).</param>
        /// <param name="expirationMinutes">How long the link is valid.</param>
        Task<string> GenerateUploadUrlAsync(string fileName, string contentType, int expirationMinutes = 60);

        /// <summary>
        /// Generates a secure, time-limited URL for viewing the video.
        /// </summary>
        Task<string> GetVideoUrlAsync(string fileName, int expirationMinutes = 120);

        /// <summary>
        /// Permanently deletes the video file.
        /// </summary>
        Task DeleteVideoAsync(string fileName);
    }
}

