import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { VideoService } from '../../../core/services/video.service';
import { VideoMetadataForm } from '../../../core/models/video.models';

@Component({
  selector: 'app-video-upload',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './video-upload.component.html'
})
export class VideoUploadComponent {
  private fb = inject(FormBuilder);
  private videoService = inject(VideoService);
  private toastr = inject(ToastrService);
  private router = inject(Router);

  selectedFile: File | null = null;
  uploadProgress = 0;
  isUploading = false;

  uploadForm = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(500)]],
    instrument: ['', Validators.required],
    category: ['Audition', Validators.required],
    isPublic: [true]
  });

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      
      // Basic validation (e.g., max 500MB)
      if (file.size > 500 * 1024 * 1024) {
        this.toastr.error('File size exceeds 500MB limit');
        input.value = '';
        return;
      }
      
      // Check type
      if (!file.type.startsWith('video/')) {
        this.toastr.error('Please select a valid video file');
        input.value = '';
        return;
      }

      this.selectedFile = file;
    }
  }

  onSubmit() {
    if (this.uploadForm.invalid || !this.selectedFile) return;

    this.isUploading = true;
    this.uploadProgress = 0;
    this.uploadForm.disable(); // Prevent changes during upload

    const metadata: VideoMetadataForm = {
      title: this.uploadForm.value.title!,
      description: this.uploadForm.value.description || '',
      instrument: this.uploadForm.value.instrument!,
      category: this.uploadForm.value.category!,
      isPublic: this.uploadForm.value.isPublic!
    };

    this.videoService.uploadVideo(this.selectedFile, metadata).subscribe({
      next: (event) => {
        if (event && event.status === 'progress') {
          this.uploadProgress = event.percent;
        } else if (event && event.status === 'complete') {
          this.toastr.success('Video uploaded successfully!');
          this.router.navigate(['/student/my-videos']);
        } else if (event && event.status === 'error') {
          this.handleError(event.error);
        }
      },
      error: (err) => this.handleError(err)
    });
  }

  private handleError(err: any) {
    console.error(err);
    this.isUploading = false;
    this.uploadForm.enable();
    this.uploadProgress = 0;
    this.toastr.error('Upload failed. Please try again.');
  }
}