import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { StudentService } from '../../services/student.service';
import { AuthService } from '../../../auth/services/auth.service';
import { RatingService } from '../../../../core/services/rating.service';
import { VideoService } from '../../../../core/services/video.service';
import { StudentDetailsDto } from '../../../../core/models/student.models';
import { CreateRatingDto, RatingDto } from '../../../../core/models/rating.models';
import { VideoDto } from '../../../../core/models/video.models';
import { Permissions, Roles } from '../../../../core/models/common.models';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { StudentStatusBadgeComponent } from '../../../../shared/components/student-status-badge/student-status-badge.component';

@Component({
  selector: 'app-student-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StudentStatusBadgeComponent],
  templateUrl: './student-detail.component.html'
})
export class StudentDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private studentService = inject(StudentService);
  private authService = inject(AuthService);
  private ratingService = inject(RatingService);
  private videoService = inject(VideoService);
  private fb = inject(FormBuilder);
  private sanitizer = inject(DomSanitizer);

  student: StudentDetailsDto | null = null;
  videos: VideoDto[] = [];
  ratings: RatingDto[] = [];
  averageRating = 0;
  
  isLoading = false;
  isRatingLoading = false;
  isSubmittingRating = false;
  error: string | null = null;
  
  canRate = false;
  canViewContact = false;
  showRatingForm = false;
  selectedVideo: VideoDto | null = null;
  safeVideoUrl: SafeResourceUrl | null = null;

  ratingForm: FormGroup = this.fb.group({
    overallRating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    musicality: [5, [Validators.min(1), Validators.max(5)]],
    technique: [5, [Validators.min(1), Validators.max(5)]],
    marchingAbility: [5, [Validators.min(1), Validators.max(5)]],
    leadership: [5, [Validators.min(1), Validators.max(5)]],
    comments: ['', Validators.maxLength(1000)]
  });

  stars = [1, 2, 3, 4, 5];
  hoveredStar = 0;

  ngOnInit(): void {
    this.canRate = this.authService.hasPermission(Permissions.RateStudents);
    this.canViewContact = this.authService.hasRole(Roles.BandStaff) || this.authService.hasRole(Roles.Director);
    
    this.loadStudent();
  }

  loadStudent(): void {
    const studentId = Number(this.route.snapshot.paramMap.get('id'));
    
    if (!studentId) {
      this.router.navigate(['/students']);
      return;
    }

    this.isLoading = true;
    this.error = null;

    this.studentService.getStudent(studentId).subscribe({
      next: (student: any) => {
        this.student = student;
        this.isLoading = false;
        this.loadVideos(studentId);
        this.loadRatings(studentId);
      },
      error: (error: any) => {
        this.error = 'Failed to load student details. Please try again.';
        this.isLoading = false;
        console.error('Error loading student:', error);
      }
    });
  }

  loadVideos(studentId: number): void {
    this.videoService.getStudentVideos(studentId).subscribe({
      next: (videos) => {
        this.videos = videos.filter(v => v.status === 'Completed');
        if (this.videos.length > 0) {
          this.selectVideo(this.videos[0]);
        }
      },
      error: (error: any) => {
        console.error('Error loading videos:', error);
      }
    });
  }

  selectVideo(video: VideoDto): void {
    this.selectedVideo = video;
    if (video.originalUrl) {
      this.safeVideoUrl = this.sanitizer.bypassSecurityTrustResourceUrl(video.originalUrl);
    }
    // Record view
    this.videoService.recordView(video.videoId).subscribe();
  }

  loadRatings(studentId: number): void {
    this.isRatingLoading = true;
    
    this.ratingService.getStudentRatings(studentId, true).subscribe({
      next: (ratings) => {
        this.ratings = ratings;
        this.calculateAverageRating();
        this.isRatingLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading ratings:', error);
        this.isRatingLoading = false;
      }
    });
  }

  calculateAverageRating(): void {
    if (this.ratings.length === 0) {
      this.averageRating = 0;
      return;
    }

    const sum = this.ratings.reduce((acc, rating) => acc + rating.overallRating, 0);
    this.averageRating = sum / this.ratings.length;
  }

  toggleRatingForm(): void {
    this.showRatingForm = !this.showRatingForm;
  }

  setRating(field: string, value: number): void {
    this.ratingForm.patchValue({ [field]: value });
  }

  setHoveredStar(value: number): void {
    this.hoveredStar = value;
  }

  clearHoveredStar(): void {
    this.hoveredStar = 0;
  }

  getStarClass(rating: number, starIndex: number): string {
    const currentRating = this.hoveredStar || rating;
    return currentRating >= starIndex ? 'text-yellow-400' : 'text-gray-300';
  }

  submitRating(): void {
    if (this.ratingForm.invalid || !this.student || !this.canRate) {
      this.markFormGroupTouched(this.ratingForm);
      return;
    }

    this.isSubmittingRating = true;

    const dto: CreateRatingDto = {
      ...this.ratingForm.value,
      studentId: this.student.studentId,
      isPublic: true
    };

    this.ratingService.createRating(dto).subscribe({
      next: () => {
        this.isSubmittingRating = false;
        this.showRatingForm = false;
        this.ratingForm.reset({
          overallRating: 5,
          musicality: 5,
          technique: 5,
          marchingAbility: 5,
          leadership: 5
        });
        this.loadRatings(this.student!.studentId);
        this.showSuccess('Rating submitted successfully');
      },
      error: (error: any) => {
        this.error = 'Failed to submit rating. Please try again.';
        this.isSubmittingRating = false;
        console.error('Error submitting rating:', error);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/students']);
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  private showSuccess(message: string): void {
    alert(message);
  }

  getRatingStars(rating: number): string {
    const fullStars = Math.floor(rating);
    const hasHalfStar = rating % 1 >= 0.5;
    let stars = '★'.repeat(fullStars);
    if (hasHalfStar) stars += '⯨';
    stars += '☆'.repeat(5 - fullStars - (hasHalfStar ? 1 : 0));
    return stars;
  }
}