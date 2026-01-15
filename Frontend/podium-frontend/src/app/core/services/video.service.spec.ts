import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { VideoService } from './video.service';

describe('VideoService', () => {
  let service: VideoService;
  let httpMock: HttpTestingController;
  const apiUrl = 'http://localhost:5000/api';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [VideoService],
    });

    service = TestBed.inject(VideoService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    // Verify that no unmatched requests are outstanding
    httpMock.verify();
  });

  describe('getVideos', () => {
    it('should return videos for a student', (done) => {
      const studentId = 1;
      const mockVideos = [
        {
          id: 1,
          title: 'Audition Video',
          url: 'https://example.com/video1.mp4',
          duration: 180,
          uploadDate: '2024-01-15',
        },
        {
          id: 2,
          title: 'Performance',
          url: 'https://example.com/video2.mp4',
          duration: 240,
          uploadDate: '2024-01-20',
        },
      ];

      service.getVideos(studentId).subscribe((videos) => {
        expect(videos).toEqual(mockVideos);
        expect(videos.length).toBe(2);
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/Students/${studentId}/videos`);
      expect(req.request.method).toBe('GET');
      req.flush(mockVideos);
    });

    it('should handle empty video list', (done) => {
      const studentId = 1;

      service.getVideos(studentId).subscribe((videos) => {
        expect(videos).toEqual([]);
        expect(videos.length).toBe(0);
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/Students/${studentId}/videos`);
      req.flush([]);
    });

    it('should handle error when fetching videos', (done) => {
      const studentId = 1;
      const errorMessage = 'Failed to load videos';

      service.getVideos(studentId).subscribe({
        next: () => {
          throw new Error('Expected error, but got success');
        },
        error: (error) => {
          expect(error.status).toBe(500);
          expect(error.statusText).toBe('Internal Server Error');
          done();
        },
      });

      const req = httpMock.expectOne(`${apiUrl}/Students/${studentId}/videos`);
      req.flush(errorMessage, { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('uploadVideo', () => {
    it('should upload video with correct FormData', (done) => {
      const studentId = 1;
      const file = new File(['video content'], 'test-video.mp4', { type: 'video/mp4' });
      const videoData = {
        title: 'Test Video',
        description: 'Test Description',
      };

      const mockResponse = {
        id: 1,
        title: 'Test Video',
        url: 'https://example.com/test-video.mp4',
      };

      service.uploadVideo(studentId, file, videoData).subscribe((response) => {
        expect(response).toEqual(mockResponse);
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/Students/${studentId}/videos`);
      expect(req.request.method).toBe('POST');
      
      // Verify FormData contains the file and metadata
      const formData = req.request.body as FormData;
      expect(formData.get('file')).toBe(file);
      expect(formData.get('title')).toBe(videoData.title);
      expect(formData.get('description')).toBe(videoData.description);

      req.flush(mockResponse);
    });

    it('should handle upload error', (done) => {
      const studentId = 1;
      const file = new File(['video content'], 'test-video.mp4', { type: 'video/mp4' });
      const videoData = { title: 'Test Video', description: 'Test' };

      service.uploadVideo(studentId, file, videoData).subscribe({
        next: () => {
          throw new Error('Expected error, but got success');
        },
        error: (error) => {
          expect(error.status).toBe(400);
          done();
        },
      });

      const req = httpMock.expectOne(`${apiUrl}/Students/${studentId}/videos`);
      req.flush('Invalid file format', { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('deleteVideo', () => {
    it('should delete video successfully', (done) => {
      const studentId = 1;
      const videoId = 5;

      service.deleteVideo(studentId, videoId).subscribe((response) => {
        expect(response).toBeDefined();
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/Students/${studentId}/videos/${videoId}`);
      expect(req.request.method).toBe('DELETE');
      req.flush({});
    });

    it('should handle delete error', (done) => {
      const studentId = 1;
      const videoId = 999;

      service.deleteVideo(studentId, videoId).subscribe({
        next: () => {
          throw new Error('Expected error, but got success');
        },
        error: (error) => {
          expect(error.status).toBe(404);
          done();
        },
      });

      const req = httpMock.expectOne(`${apiUrl}/Students/${studentId}/videos/${videoId}`);
      req.flush('Video not found', { status: 404, statusText: 'Not Found' });
    });
  });

  describe('validateFile', () => {
    it('should validate video file format', () => {
      const validFile = new File(['content'], 'video.mp4', { type: 'video/mp4' });
      expect(service.validateFile(validFile)).toBe(true);
    });

    it('should reject invalid file format', () => {
      const invalidFile = new File(['content'], 'document.pdf', { type: 'application/pdf' });
      expect(service.validateFile(invalidFile)).toBe(false);
    });

    it('should reject file exceeding size limit', () => {
      // Create a file larger than 100MB
      const largeFile = new File(['x'.repeat(101 * 1024 * 1024)], 'large.mp4', {
        type: 'video/mp4',
      });
      expect(service.validateFile(largeFile)).toBe(false);
    });

    it('should accept file within size limit', () => {
      // Create a file of 50MB
      const validFile = new File(['x'.repeat(50 * 1024 * 1024)], 'valid.mp4', {
        type: 'video/mp4',
      });
      expect(service.validateFile(validFile)).toBe(true);
    });
  });

  describe('formatDuration', () => {
    it('should format seconds to mm:ss', () => {
      expect(service.formatDuration(0)).toBe('0:00');
      expect(service.formatDuration(30)).toBe('0:30');
      expect(service.formatDuration(90)).toBe('1:30');
      expect(service.formatDuration(3665)).toBe('61:05');
    });
  });

  describe('formatFileSize', () => {
    it('should format bytes to human-readable size', () => {
      expect(service.formatFileSize(0)).toBe('0 B');
      expect(service.formatFileSize(1024)).toBe('1.0 KB');
      expect(service.formatFileSize(1024 * 1024)).toBe('1.0 MB');
      expect(service.formatFileSize(1024 * 1024 * 1024)).toBe('1.0 GB');
      expect(service.formatFileSize(1536 * 1024)).toBe('1.5 MB');
    });
  });
});
