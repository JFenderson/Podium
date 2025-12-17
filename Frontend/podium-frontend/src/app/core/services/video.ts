import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { ApiService } from '../../core/services/api';
import {
  VideoDto,
  CreateVideoDto,
  UpdateVideoDto,
  VideoUploadDto,
  VideoUploadProgressDto,
  VideoProcessingStatusDto,
  VideoFilterDto,
  VideoSummaryDto,
  VideoAnalyticsDto,
  VideoThumbnailDto,
  GenerateThumbnailDto
} from '../models/video';
import { PagedResult } from '../../core/models/student';

@Injectable({
  providedIn: 'root'
})
export class VideoService {
  private readonly endpoint = 'Videos';
  private uploadProgress = new Subject<VideoUploadProgressDto>();
  public uploadProgress$ = this.uploadProgress.asObservable();

  constructor(private api: ApiService) {}

  /**
   * Get all videos with filtering
   */
  getVideos(filter?: VideoFilterDto): Observable<PagedResult<VideoDto>> {
    return this.api.get<PagedResult<VideoDto>>(this.endpoint, filter);
  }

  /**
   * Get video by ID
   */
  getVideo(id: number): Observable<VideoDto> {
    return this.api.get<VideoDto>(`${this.endpoint}/${id}`);
  }

  /**
   * Get videos by student
   */
  getStudentVideos(studentId: number): Observable<VideoDto[]> {
    return this.api.get<VideoDto[]>(`${this.endpoint}/student/${studentId}`);
  }

  /**
   * Get my videos (current student)
   */
  getMyVideos(): Observable<VideoDto[]> {
    return this.api.get<VideoDto[]>(`${this.endpoint}/my-videos`);
  }

  /**
   * Upload video
   */
  uploadVideo(dto: VideoUploadDto): Observable<VideoDto> {
    const formData = new FormData();
    formData.append('videoFile', dto.videoFile);
    formData.append('studentId', dto.studentId.toString());
    formData.append('title', dto.title);
    
    if (dto.description) {
      formData.append('description', dto.description);
    }
    
    if (dto.isPublic !== undefined) {
      formData.append('isPublic', dto.isPublic.toString());
    }

    return this.api.upload<VideoDto>(`${this.endpoint}/upload`, formData);
  }

  /**
   * Upload video with progress tracking
   */
  uploadVideoWithProgress(dto: VideoUploadDto): Observable<VideoUploadProgressDto | VideoDto> {
    const formData = new FormData();
    formData.append('videoFile', dto.videoFile);
    formData.append('studentId', dto.studentId.toString());
    formData.append('title', dto.title);
    
    if (dto.description) {
      formData.append('description', dto.description);
    }
    
    if (dto.isPublic !== undefined) {
      formData.append('isPublic', dto.isPublic.toString());
    }

    // This would need custom HttpClient configuration to track progress
    // For now, using standard upload
    return this.api.upload<VideoDto>(`${this.endpoint}/upload`, formData);
  }

  /**
   * Update video metadata
   */
  updateVideo(id: number, dto: UpdateVideoDto): Observable<any> {
    return this.api.put(`${this.endpoint}/${id}`, dto);
  }

  /**
   * Delete video
   */
  deleteVideo(id: number): Observable<any> {
    return this.api.delete(`${this.endpoint}/${id}`);
  }

  /**
   * Get video processing status
   */
  getProcessingStatus(videoId: number): Observable<VideoProcessingStatusDto> {
    return this.api.get<VideoProcessingStatusDto>(`${this.endpoint}/${videoId}/status`);
  }

  /**
   * Get video analytics
   */
  getVideoAnalytics(videoId: number): Observable<VideoAnalyticsDto> {
    return this.api.get<VideoAnalyticsDto>(`${this.endpoint}/${videoId}/analytics`);
  }

  /**
   * Increment video view count
   */
  recordView(videoId: number): Observable<any> {
    return this.api.post(`${this.endpoint}/${videoId}/view`, {});
  }

  /**
   * Get video thumbnails
   */
  getVideoThumbnails(videoId: number): Observable<VideoThumbnailDto[]> {
    return this.api.get<VideoThumbnailDto[]>(`${this.endpoint}/${videoId}/thumbnails`);
  }

  /**
   * Generate thumbnail at specific time
   */
  generateThumbnail(dto: GenerateThumbnailDto): Observable<VideoThumbnailDto> {
    return this.api.post<VideoThumbnailDto>(`${this.endpoint}/${dto.videoId}/generate-thumbnail`, {
      timeOffset: dto.timeOffset
    });
  }

  /**
   * Set primary thumbnail
   */
  setPrimaryThumbnail(videoId: number, thumbnailId: number): Observable<any> {
    return this.api.post(`${this.endpoint}/${videoId}/thumbnails/${thumbnailId}/set-primary`, {});
  }

  /**
   * Search videos
   */
  searchVideos(searchTerm: string, studentId?: number): Observable<VideoSummaryDto[]> {
    return this.api.get<VideoSummaryDto[]>(`${this.endpoint}/search`, {
      search: searchTerm,
      studentId
    });
  }

  /**
   * Get popular videos
   */
  getPopularVideos(limit: number = 10): Observable<VideoSummaryDto[]> {
    return this.api.get<VideoSummaryDto[]>(`${this.endpoint}/popular`, { limit });
  }

  /**
   * Get recent videos
   */
  getRecentVideos(limit: number = 10): Observable<VideoSummaryDto[]> {
    return this.api.get<VideoSummaryDto[]>(`${this.endpoint}/recent`, { limit });
  }

  /**
   * Get videos by band (videos from students of interest)
   */
  getVideosByBand(bandId: number): Observable<VideoDto[]> {
    return this.api.get<VideoDto[]>(`${this.endpoint}/band/${bandId}`);
  }

  /**
   * Share video
   */
  shareVideo(videoId: number, emails: string[], message?: string): Observable<any> {
    return this.api.post(`${this.endpoint}/${videoId}/share`, {
      emails,
      message
    });
  }

  /**
   * Report video
   */
  reportVideo(videoId: number, reason: string): Observable<any> {
    return this.api.post(`${this.endpoint}/${videoId}/report`, { reason });
  }

  /**
   * Get reported videos (Admin/Director)
   */
  getReportedVideos(): Observable<VideoDto[]> {
    return this.api.get<VideoDto[]>(`${this.endpoint}/reported`);
  }

  /**
   * Resolve reported video
   */
  resolveReportedVideo(videoId: number, action: 'approve' | 'remove', notes?: string): Observable<any> {
    return this.api.post(`${this.endpoint}/${videoId}/resolve-report`, { action, notes });
  }

  /**
   * Get video quality variants
   */
  getVideoVariants(videoId: number): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/${videoId}/variants`);
  }

  /**
   * Retry failed video processing
   */
  retryProcessing(videoId: number): Observable<any> {
    return this.api.post(`${this.endpoint}/${videoId}/retry-processing`, {});
  }

  /**
   * Get video statistics by student
   */
  getStudentVideoStats(studentId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/student/${studentId}/stats`);
  }

  /**
   * Download video
   */
  downloadVideo(videoId: number, quality: string = 'original'): Observable<Blob> {
    return this.api.download(`${this.endpoint}/${videoId}/download`, { quality });
  }

  /**
   * Get video embed code
   */
  getEmbedCode(videoId: number): Observable<{ embedCode: string }> {
    return this.api.get<{ embedCode: string }>(`${this.endpoint}/${videoId}/embed`);
  }

  /**
   * Validate video file before upload
   */
  validateVideoFile(file: File): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];
    
    // Check file size (500MB max)
    if (file.size > 500 * 1024 * 1024) {
      errors.push('Video file must be less than 500MB');
    }

    // Check file type
    const allowedTypes = ['video/mp4', 'video/quicktime', 'video/x-msvideo', 'video/x-ms-wmv', 'video/webm'];
    if (!allowedTypes.includes(file.type)) {
      errors.push('Invalid video format. Allowed formats: MP4, MOV, AVI, WMV, WEBM');
    }

    return {
      isValid: errors.length === 0,
      errors
    };
  }
}