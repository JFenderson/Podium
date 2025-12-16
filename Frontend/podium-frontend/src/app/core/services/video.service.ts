import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEventType, HttpHeaders } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError, concatMap, map, switchMap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { 
  GetUploadRequest, 
  GetUploadResponse, 
  CreateVideoRequest, 
  VideoResponse, 
  VideoMetadataForm,
  UploadStatus 
} from '../models/video.models';

@Injectable({
  providedIn: 'root'
})
export class VideoService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/video`;

  /**
   * Orchestrates the 3-step upload process:
   * 1. Get Pre-signed URL
   * 2. Upload binary to Cloud Storage (with progress)
   * 3. Register Video in Backend
   */
  uploadVideo(file: File, metadata: VideoMetadataForm): Observable<UploadStatus> {
    
    // Step 1 Payload
    const initRequest: GetUploadRequest = {
      fileName: file.name,
      contentType: file.type,
      fileSizeBytes: file.size
    };

    return this.http.post<GetUploadResponse>(`${this.apiUrl}/upload-url`, initRequest).pipe(
      switchMap(config => {
        // Step 2: PUT file to the pre-signed URL (Bypassing our API)
        // We do NOT use the API interceptor here because it's an external URL (AWS/Azure)
        // Ideally, the interceptor logic checks request.url.startsWith(apiUrl)
        
        return this.http.put(config.uploadUrl, file, {
          headers: new HttpHeaders({ 'Content-Type': file.type }),
          reportProgress: true,
          observe: 'events'
        }).pipe(
          concatMap(event => {
            // Handle Progress Events
            if (event.type === HttpEventType.UploadProgress && event.total) {
              const percent = Math.round((100 * event.loaded) / event.total);
              return of<UploadStatus>({ status: 'progress', percent });
            } 
            
            // Handle Completion -> Trigger Step 3
            if (event.type === HttpEventType.Response) {
              const finalizeRequest: CreateVideoRequest = {
                uploadId: config.uploadId,
                title: metadata.title,
                description: metadata.description,
                instrument: metadata.instrument,
                category: metadata.category,
                isPublic: metadata.isPublic,
                fileName: config.storagePath, // IMPORTANT: Backend expects storage path here
                contentType: file.type,
                fileSizeBytes: file.size
              };

              return this.http.post<VideoResponse>(this.apiUrl, finalizeRequest).pipe(
                map(video => ({ status: 'complete', data: video } as UploadStatus))
              );
            }

            // Ignore other events (Sent, HeadersReceived)
            return of<UploadStatus>(); 
          })
        );
      }),
      catchError(err => of<UploadStatus>({ status: 'error', error: err }))
    );
  }
}