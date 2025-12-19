import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { VideoService } from '../../../../core/services/video';
import { AuthService } from '../../../../features/auth/services/auth';
import { VideoUploadDto, VideoRules, VideoDto, VideoUploadProgressDto } from '../../../../core/models/video';
import { Roles } from '../../../../core/models/common';

@Component({
  selector: 'app-video-upload',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './video-upload.html'
})
export class VideoUploadComponent implements OnInit {
  private videoService = inject(VideoService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  uploadForm: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(1000)],
    isPublic: [true]
  });

  selectedFile: File | null = null;
  isUploading = false;
  uploadProgress = 0;
  error: string | null = null;
  validationErrors: string[] = [];
  isStudent = false;

  // File constraints
  maxFileSize = VideoRules.maxFileSize; // 500MB in bytes
  maxDuration = VideoRules.maxDuration; // 600 seconds (10 minutes)
  allowedFormats = VideoRules.allowedFormats;
  allowedMimeTypes = VideoRules.allowedMimeTypes;

  ngOnInit(): void {
    this.isStudent = this.authService.hasRole(Roles.Student);
    
    if (!this.isStudent) {
      this.error = 'Access denied. Only students can upload audition videos.';
      return;
    }
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    this.selectedFile = null;
    this.validationErrors = [];
    this.error = null;

    // Validate file
    const validation = this.videoService.validateVideoFile(file);
    
    if (!validation.isValid) {
      this.validationErrors = validation.errors;
      this.error = 'Please select a valid video file';
      return;
    }

    this.selectedFile = file;
  }

  removeFile(): void {
    this.selectedFile = null;
    this.validationErrors = [];
    this.error = null;
  }

  uploadVideo(): void {
    if (this.uploadForm.invalid || !this.selectedFile) {
      this.markFormGroupTouched(this.uploadForm);
      if (!this.selectedFile) {
        this.error = 'Please select a video file';
      }
      return;
    }

    const currentUser = this.authService.currentUserValue;
    if (!currentUser?.studentId) {
      this.error = 'Student profile not found';
      return;
    }

    this.isUploading = true;
    this.uploadProgress = 0;
    this.error = null;

    const uploadDto: VideoUploadDto = {
      studentId: currentUser.studentId,
      title: this.uploadForm.value.title,
      description: this.uploadForm.value.description,
      videoFile: this.selectedFile,
      isPublic: this.uploadForm.value.isPublic
    };

    // Use upload with progress tracking
  this.videoService.uploadVideoWithProgress(uploadDto).subscribe({
    next: (event) => {
      // Use type assertion after checking
      if ('type' in event && event.type === 'progress') {
        const progressEvent = event as VideoUploadProgressDto;
        this.uploadProgress = progressEvent.progress || 0;
      } else if ('type' in event && event.type === 'complete') {
        this.isUploading = false;
        this.uploadProgress = 100;
        this.router.navigate(['/students/profile']);
      }
    },
    error: (error) => {
      this.error = 'Failed to upload video. Please try again.';
      this.isUploading = false;
      this.uploadProgress = 0;
      console.error('Error uploading video:', error);
    }
  });
  }

  cancel(): void {
    if (this.isUploading) {
      if (confirm('Upload in progress. Are you sure you want to cancel?')) {
        this.router.navigate(['/students/profile']);
      }
    } else {
      this.router.navigate(['/students/profile']);
    }
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  getMaxFileSizeFormatted(): string {
    return this.formatFileSize(this.maxFileSize);
  }

  getMaxDurationFormatted(): string {
    const minutes = Math.floor(this.maxDuration / 60);
    const seconds = this.maxDuration % 60;
    return seconds > 0 ? `${minutes}:${seconds.toString().padStart(2, '0')}` : `${minutes}:00`;
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }
}