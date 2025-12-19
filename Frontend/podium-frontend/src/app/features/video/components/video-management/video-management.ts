import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { VideoService } from '../../../../core/services/video';
import { AuthService } from '../../../../features/auth/services/auth';
import { VideoDto, VideoFilterDto, VideoStatus } from '../../../../core/models/video';
import { PagedResult, Permissions } from '../../../../core/models/common';

@Component({
  selector: 'app-video-management',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './video-management.html'
})
export class VideoManagementComponent implements OnInit {
  private videoService = inject(VideoService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  videos: VideoDto[] = [];
  isLoading = false;
  error: string | null = null;
  successMessage: string | null = null;
  canViewStudents = false;

  filterForm: FormGroup = this.fb.group({
    search: [''],
    status: [''],
    studentId: [null]
  });

  statuses: VideoStatus[] = ['Pending', 'Uploading', 'Processing', 'Completed', 'Failed', 'Deleted'];

  ngOnInit(): void {
    this.canViewStudents = this.authService.hasPermission(Permissions.ViewStudents);
    
    if (!this.canViewStudents) {
      this.error = 'Access denied. You do not have permission to view student videos.';
      return;
    }

    this.loadVideos();
    this.setupFilterListeners();
  }

loadVideos(): void {
  this.isLoading = true;
  this.error = null;

  const filter: VideoFilterDto = {
    ...this.filterForm.value
  };

  Object.keys(filter).forEach(key => {
    if (!filter[key as keyof VideoFilterDto]) {
      delete filter[key as keyof VideoFilterDto];
    }
  });

  this.videoService.getVideos(filter).subscribe({
    next: (result: PagedResult<VideoDto>) => {
      this.videos = result.items; // Extract items from paged result
      this.isLoading = false;
    },
    error: (error) => {
      this.error = 'Failed to load videos. Please try again.';
      this.isLoading = false;
      console.error('Error loading videos:', error);
    }
  });
}

  setupFilterListeners(): void {
    this.filterForm.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.loadVideos();
      });
  }

  clearFilters(): void {
    this.filterForm.reset();
  }

  retryProcessing(videoId: number): void {
    if (!confirm('Retry processing this video?')) {
      return;
    }

    this.videoService.retryProcessing(videoId).subscribe({
      next: () => {
        this.successMessage = 'Video queued for reprocessing';
        this.loadVideos();
        setTimeout(() => this.successMessage = null, 3000);
      },
      error: (error) => {
        this.error = 'Failed to retry processing. Please try again.';
        console.error('Error retrying processing:', error);
      }
    });
  }

  deleteVideo(videoId: number, videoTitle: string): void {
    if (!confirm(`Are you sure you want to delete "${videoTitle}"? This action cannot be undone.`)) {
      return;
    }

    this.videoService.deleteVideo(videoId).subscribe({
      next: () => {
        this.successMessage = 'Video deleted successfully';
        this.loadVideos();
        setTimeout(() => this.successMessage = null, 3000);
      },
      error: (error) => {
        this.error = 'Failed to delete video. Please try again.';
        console.error('Error deleting video:', error);
      }
    });
  }

  getStatusColor(status: VideoStatus): string {
    const colors: { [key in VideoStatus]: string } = {
      'Pending': 'bg-gray-100 text-gray-800',
      'Uploading': 'bg-blue-100 text-blue-800',
      'Processing': 'bg-yellow-100 text-yellow-800',
      'Completed': 'bg-green-100 text-green-800',
      'Failed': 'bg-red-100 text-red-800',
      'Deleted': 'bg-gray-100 text-gray-500'
    };
    return colors[status] || 'bg-gray-100 text-gray-800';
  }

  getStatusIcon(status: VideoStatus): string {
    const icons: { [key in VideoStatus]: string } = {
      'Pending': 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
      'Uploading': 'M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12',
      'Processing': 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15',
      'Completed': 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z',
      'Failed': 'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z',
      'Deleted': 'M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16'
    };
    return icons[status] || icons['Pending'];
  }

  formatDuration(seconds?: number): string {
    if (!seconds) return 'N/A';
    const minutes = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${minutes}:${secs.toString().padStart(2, '0')}`;
  }

  formatFileSize(bytes?: number): string {
    if (!bytes) return 'N/A';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

    hasProcessingVideos(): boolean {
    return this.videos.some(v => v.status === 'Processing');
  }
}