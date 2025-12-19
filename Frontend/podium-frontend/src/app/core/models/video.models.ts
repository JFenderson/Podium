// Video DTOs matching backend

export interface VideoDto {
  videoId: number;
  studentId: number;
  studentName?: string;
  title: string;
  description?: string;
  originalUrl: string;
  thumbnailUrl?: string;
  duration?: number; // in seconds
  fileSize?: number; // in bytes
  variants?: VideoVariantDto[];
  status: VideoStatus;
  processingProgress?: number; // 0-100
  errorMessage?: string;
  uploadedAt: Date;
  processedAt?: Date;
  viewCount: number;
  isPublic: boolean;
  createdAt: Date;
  updatedAt?: Date;
}

export interface CreateVideoDto {
  studentId: number;
  title: string;
  description?: string;
  isPublic?: boolean;
}

export interface UpdateVideoDto {
  title?: string;
  description?: string;
  isPublic?: boolean;
}

export interface VideoVariantDto {
  variantId?: number;
  quality: VideoQuality;
  url: string;
  fileSize?: number;
  width?: number;
  height?: number;
  bitrate?: number;
}

export enum VideoQuality {
  Original = 'Original',
  High = '1080p',
  Medium = '720p',
  Low = '480p',
  Mobile = '360p'
}

export type VideoStatus = 'Pending' | 'Uploading' | 'Processing' | 'Completed' | 'Failed' | 'Deleted';
export interface VideoUploadDto {
  studentId: number;
  title: string;
  description?: string;
  videoFile: File;
  isPublic?: boolean;
}

export interface VideoUploadProgressDto {
  videoId: number;
  bytesUploaded: number;
  totalBytes: number;
  percentage: number;
  status: UploadStatus;
  type: 'progress' | 'complete';
  data?: VideoDto;
  progress: number; 
}

export enum UploadStatus {
  Initializing = 'Initializing',
  Uploading = 'Uploading',
  Finalizing = 'Finalizing',
  Completed = 'Completed',
  Failed = 'Failed',
  Cancelled = 'Cancelled'
}

export interface VideoProcessingStatusDto {
  videoId: number;
  status: VideoStatus;
  progress: number;
  currentStep?: string;
  estimatedTimeRemaining?: number; // in seconds
  errorMessage?: string;
}

export interface VideoFilterDto {
  studentId?: number;
  status?: VideoStatus;
  isPublic?: boolean;
  uploadedFrom?: Date;
  uploadedTo?: Date;
  search?: string;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: VideoSortBy;
  sortOrder?: 'asc' | 'desc';
}

export enum VideoSortBy {
  UploadDate = 'UploadDate',
  Title = 'Title',
  ViewCount = 'ViewCount',
  Duration = 'Duration'
}

export interface VideoSummaryDto {
  videoId: number;
  title: string;
  thumbnailUrl?: string;
  duration?: number;
  status: VideoStatus;
  uploadedAt: Date;
  viewCount: number;
}

export interface VideoAnalyticsDto {
  videoId: number;
  totalViews: number;
  uniqueViews: number;
  averageViewDuration: number;
  completionRate: number;
  viewsByDate: ViewsByDateDto[];
  viewsByBand: ViewsByBandDto[];
}

export interface ViewsByDateDto {
  date: Date;
  views: number;
}

export interface ViewsByBandDto {
  bandId: number;
  bandName: string;
  views: number;
}

export interface VideoMetadataDto {
  width: number;
  height: number;
  duration: number;
  bitrate: number;
  codec: string;
  frameRate: number;
  aspectRatio: string;
}

export interface VideoThumbnailDto {
  thumbnailId: number;
  videoId: number;
  url: string;
  width: number;
  height: number;
  timeOffset: number; // seconds into video
  isPrimary: boolean;
}

export interface GenerateThumbnailDto {
  videoId: number;
  timeOffset: number;
}

// Video validation rules
export const VideoRules = {
  maxFileSize: 500 * 1024 * 1024, // 500MB
  maxDuration: 600, // 10 minutes in seconds
  allowedFormats: ['.mp4', '.mov', '.avi', '.wmv', '.webm'],
  allowedMimeTypes: ['video/mp4', 'video/quicktime', 'video/x-msvideo', 'video/x-ms-wmv', 'video/webm'],
  maxTitleLength: 200,
  maxDescriptionLength: 2000
} as const;