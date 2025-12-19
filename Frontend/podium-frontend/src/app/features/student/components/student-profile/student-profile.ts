import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { StudentService } from '../../services/student';
import { VideoService } from '../../../../core/services/video';
import { AuthService } from '../../../../features/auth/services/auth';
import { StudentDetailsDto, UpdateStudentDto } from '../../../../core/models/student';
import { VideoDto, VideoUploadDto } from '../../../../core/models/video';
import { Roles } from '../../../../core/models/common';

@Component({
  selector: 'app-student-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './student-profile.html'
})
export class StudentProfileComponent implements OnInit {
  private studentService = inject(StudentService);
  private videoService = inject(VideoService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  student: StudentDetailsDto | null = null;
  videos: VideoDto[] = [];
  isLoading = false;
  isSaving = false;
  isUploadingVideo = false;
  error: string | null = null;
  successMessage: string | null = null;
  isStudent = false;

  profileForm: FormGroup = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    bio: ['', Validators.maxLength(2000)],
    primaryInstrument: ['', Validators.required],
    secondaryInstruments: [''],
    yearsOfExperience: [null, [Validators.min(0), Validators.max(20)]],
    highSchool: ['', Validators.required],
    graduationYear: [null, Validators.required],
    gpa: [null, [Validators.min(0), Validators.max(4.0)]],
    city: [''],
    state: [''],
    zipCode: ['']
  });

  videoUploadForm: FormGroup = this.fb.group({
    title: ['', Validators.required],
    description: [''],
    isPublic: [true]
  });

  instruments = ['Trumpet', 'Trombone', 'Tuba', 'Saxophone', 'Clarinet', 'Flute', 
                 'Percussion', 'Mellophone', 'Baritone', 'French Horn', 'Piccolo'];
  
  states = ['AL', 'AK', 'AZ', 'AR', 'CA', 'CO', 'CT', 'DE', 'FL', 'GA', 'HI', 'ID', 
            'IL', 'IN', 'IA', 'KS', 'KY', 'LA', 'ME', 'MD', 'MA', 'MI', 'MN', 'MS', 
            'MO', 'MT', 'NE', 'NV', 'NH', 'NJ', 'NM', 'NY', 'NC', 'ND', 'OH', 'OK', 
            'OR', 'PA', 'RI', 'SC', 'SD', 'TN', 'TX', 'UT', 'VT', 'VA', 'WA', 'WV', 
            'WI', 'WY'];

  graduationYears: number[] = [];
  selectedVideoFile: File | null = null;

  ngOnInit(): void {
    this.isStudent = this.authService.hasRole(Roles.Student);
    
    if (!this.isStudent) {
      this.error = 'Access denied. Student role required.';
      return;
    }

    this.initializeGraduationYears();
    this.loadProfile();
  }

  initializeGraduationYears(): void {
    const currentYear = new Date().getFullYear();
    for (let i = 0; i < 6; i++) {
      this.graduationYears.push(currentYear + i);
    }
  }

  loadProfile(): void {
    this.isLoading = true;
    this.error = null;

    this.studentService.getMyProfile().subscribe({
      next: (student) => {
        this.student = student;
        this.populateForm(student);
        this.loadVideos(student.studentId);
        this.isLoading = false;
      },
      error: (error) => {
        this.error = 'Failed to load profile. Please try again.';
        this.isLoading = false;
        console.error('Error loading profile:', error);
      }
    });
  }

  populateForm(student: StudentDetailsDto): void {
    this.profileForm.patchValue({
      firstName: student.firstName,
      lastName: student.lastName,
      email: student.email,
      phoneNumber: student.phoneNumber,
      bio: student.bio,
      primaryInstrument: student.primaryInstrument,
      secondaryInstruments: student.secondaryInstruments?.join(', '),
      yearsOfExperience: student.yearsOfExperience,
      highSchool: student.highSchool,
      graduationYear: student.graduationYear,
      gpa: student.gpa,
      city: student.city,
      state: student.state,
      zipCode: student.zipcode
    });
  }

  loadVideos(studentId: number): void {
    this.videoService.getStudentVideos(studentId).subscribe({
      next: (videos) => {
        this.videos = videos;
      },
      error: (error) => {
        console.error('Error loading videos:', error);
      }
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid || !this.student) {
      this.markFormGroupTouched(this.profileForm);
      return;
    }

    this.isSaving = true;
    this.error = null;
    this.successMessage = null;

    const formValue = this.profileForm.value;
    const dto: UpdateStudentDto = {
      ...formValue,
      secondaryInstruments: formValue.secondaryInstruments 
        ? formValue.secondaryInstruments.split(',').map((s: string) => s.trim()).filter((s: string) => s)
        : []
    };

    this.studentService.updateStudent(this.student.studentId, dto).subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = 'Profile updated successfully!';
        this.loadProfile();
        setTimeout(() => this.successMessage = null, 3000);
      },
      error: (error) => {
        this.error = 'Failed to update profile. Please try again.';
        this.isSaving = false;
        console.error('Error updating profile:', error);
      }
    });
  }

  onVideoFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    // Validate file
    const validation = this.videoService.validateVideoFile(file);
    if (!validation.isValid) {
      this.error = validation.errors.join(', ');
      return;
    }

    this.selectedVideoFile = file;
    this.error = null;
  }

  uploadVideo(): void {
    if (this.videoUploadForm.invalid || !this.selectedVideoFile || !this.student) {
      this.markFormGroupTouched(this.videoUploadForm);
      return;
    }

    this.isUploadingVideo = true;
    this.error = null;
    this.successMessage = null;

    const uploadDto: VideoUploadDto = {
      studentId: this.student.studentId,
      title: this.videoUploadForm.value.title,
      description: this.videoUploadForm.value.description,
      videoFile: this.selectedVideoFile,
      isPublic: this.videoUploadForm.value.isPublic
    };

    this.videoService.uploadVideo(uploadDto).subscribe({
      next: () => {
        this.isUploadingVideo = false;
        this.successMessage = 'Video uploaded successfully! Processing may take a few minutes.';
        this.videoUploadForm.reset({ isPublic: true });
        this.selectedVideoFile = null;
        this.loadVideos(this.student!.studentId);
        setTimeout(() => this.successMessage = null, 5000);
      },
      error: (error) => {
        this.error = 'Failed to upload video. Please try again.';
        this.isUploadingVideo = false;
        console.error('Error uploading video:', error);
      }
    });
  }

  deleteVideo(videoId: number): void {
    if (!confirm('Are you sure you want to delete this video? This action cannot be undone.')) {
      return;
    }

    this.videoService.deleteVideo(videoId).subscribe({
      next: () => {
        this.successMessage = 'Video deleted successfully';
        this.videos = this.videos.filter(v => v.videoId !== videoId);
        setTimeout(() => this.successMessage = null, 3000);
      },
      error: (error) => {
        this.error = 'Failed to delete video. Please try again.';
        console.error('Error deleting video:', error);
      }
    });
  }

  getVideoStatusColor(status: string): string {
    const colors: { [key: string]: string } = {
      'Completed': 'bg-green-100 text-green-800',
      'Processing': 'bg-yellow-100 text-yellow-800',
      'Pending': 'bg-gray-100 text-gray-800',
      'Failed': 'bg-red-100 text-red-800'
    };
    return colors[status] || 'bg-gray-100 text-gray-800';
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }
}