export interface GetUploadRequest {
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
}

export interface GetUploadResponse {
  uploadUrl: string;
  storagePath: string;
  uploadId: string;
  expiresAt: string;
}

export interface CreateVideoRequest {
  uploadId: string;
  title: string;
  description?: string;
  instrument: string;
  isPublic: boolean;
  fileName: string; // This will map to storagePath
  contentType: string;
  fileSizeBytes: number;
  category?: string;
}

export interface VideoResponse {
  videoId: number;
  title: string;
  videoUrl: string;
}

// Helper type for the Frontend Stream
export type UploadStatus = 
  | { status: 'progress'; percent: number }
  | { status: 'complete'; data: VideoResponse }
  | { status: 'error'; error: any };

export interface VideoMetadataForm {
  title: string;
  description: string;
  instrument: string;
  category: string;
  isPublic: boolean;
}