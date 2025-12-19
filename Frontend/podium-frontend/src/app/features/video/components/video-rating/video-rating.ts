import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { RatingService } from '../../../../core/services/rating';
import { AuthService } from '../../../../features/auth/services/auth';
import { VideoDto } from '../../../../core/models/video';
import { RatingDto, CreateRatingDto } from '../../../../core/models/rating';
import { Roles } from '../../../../core/models/common';

@Component({
  selector: 'app-video-player-rating',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './video-rating.html'
})
export class VideoPlayerRatingComponent implements OnInit {
  @Input() video!: VideoDto;
  @Input() studentId!: number;

  private ratingService = inject(RatingService);
  private authService = inject(AuthService);
  private sanitizer = inject(DomSanitizer);
  private fb = inject(FormBuilder);

  safeVideoUrl: SafeResourceUrl | null = null;
  ratings: RatingDto[] = [];
  averageRating = 0;
  isLoading = false;
  isSubmitting = false;
  error: string | null = null;
  canRate = false;

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
    this.canRate = this.authService.hasRole(Roles.BandStaff) || this.authService.hasRole(Roles.Director);
    this.sanitizeVideoUrl();
    this.loadRatings();
  }

  sanitizeVideoUrl(): void {
    if (this.video?.originalUrl) {
      this.safeVideoUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.video.originalUrl);
    }
  }

  loadRatings(): void {
    if (!this.studentId) return;

    this.isLoading = true;
    this.error = null;

    this.ratingService.getStudentRatings(this.studentId, true).subscribe({
      next: (ratings) => {
        this.ratings = ratings;
        this.calculateAverageRating();
        this.isLoading = false;
      },
      error: (error) => {
        this.error = 'Failed to load ratings';
        this.isLoading = false;
        console.error('Error loading ratings:', error);
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

  setRating(field: string, value: number): void {
    this.ratingForm.patchValue({ [field]: value });
  }

  setHoveredStar(value: number): void {
    this.hoveredStar = value;
  }

  clearHoveredStar(): void {
    this.hoveredStar = 0;
  }

  submitRating(): void {
    if (this.ratingForm.invalid || !this.canRate) {
      this.markFormGroupTouched(this.ratingForm);
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    const dto: CreateRatingDto = {
      ...this.ratingForm.value,
      studentId: this.studentId,
      isPublic: true
    };

    this.ratingService.createRating(dto).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.ratingForm.reset({
          overallRating: 5,
          musicality: 5,
          technique: 5,
          marchingAbility: 5,
          leadership: 5
        });
        this.loadRatings();
        this.showSuccess('Rating submitted successfully');
      },
      error: (error) => {
        this.error = 'Failed to submit rating. Please try again.';
        this.isSubmitting = false;
        console.error('Error submitting rating:', error);
      }
    });
  }

  getStarClass(rating: number, starIndex: number): string {
    const currentRating = this.hoveredStar || rating;
    return currentRating >= starIndex ? 'text-yellow-400' : 'text-gray-300';
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
}