import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import {
  RatingDto,
  CreateRatingDto,
  UpdateRatingDto,
  StudentRatingSummaryDto,
  BandStaffRatingStatsDto,
  RatingFilterDto,
  RatingSummaryDto,
  RatingValidationDto
} from '../models/rating.models';
import { PagedResult } from '../../core/models/student.models';

@Injectable({
  providedIn: 'root'
})
export class RatingService {
  private readonly endpoint = 'Ratings';

  constructor(private api: ApiService) {}

  /**
   * Get all ratings with filtering
   */
  getRatings(filter?: RatingFilterDto): Observable<PagedResult<RatingDto>> {
    return this.api.get<PagedResult<RatingDto>>(this.endpoint, filter);
  }

  /**
   * Get rating by ID
   */
  getRating(id: number): Observable<RatingDto> {
    return this.api.get<RatingDto>(`${this.endpoint}/${id}`);
  }

  /**
   * Create new rating (BandStaff with permission)
   */
  createRating(dto: CreateRatingDto): Observable<RatingDto> {
    return this.api.post<RatingDto>(this.endpoint, dto);
  }

  /**
   * Update rating (own rating only)
   */
  updateRating(id: number, dto: UpdateRatingDto): Observable<any> {
    return this.api.put(`${this.endpoint}/${id}`, dto);
  }

  /**
   * Delete rating (own rating only)
   */
  deleteRating(id: number): Observable<any> {
    return this.api.delete(`${this.endpoint}/${id}`);
  }

  /**
   * Get ratings for a student
   */
  getStudentRatings(studentId: number, isPublic?: boolean): Observable<RatingDto[]> {
    return this.api.get<RatingDto[]>(`${this.endpoint}/student/${studentId}`, { isPublic });
  }

  /**
   * Get student rating summary
   */
  getStudentRatingSummary(studentId: number): Observable<StudentRatingSummaryDto> {
    return this.api.get<StudentRatingSummaryDto>(`${this.endpoint}/student/${studentId}/summary`);
  }

  /**
   * Get ratings by band staff
   */
  getRatingsByStaff(staffId: number): Observable<RatingDto[]> {
    return this.api.get<RatingDto[]>(`${this.endpoint}/staff/${staffId}`);
  }

  /**
   * Get band staff rating statistics
   */
  getStaffRatingStats(staffId: number): Observable<BandStaffRatingStatsDto> {
    return this.api.get<BandStaffRatingStatsDto>(`${this.endpoint}/staff/${staffId}/stats`);
  }

  /**
   * Get my ratings (current band staff)
   */
  getMyRatings(): Observable<RatingDto[]> {
    return this.api.get<RatingDto[]>(`${this.endpoint}/my-ratings`);
  }

  /**
   * Get ratings I've received (current student)
   */
  getMyReceivedRatings(): Observable<RatingDto[]> {
    return this.api.get<RatingDto[]>(`${this.endpoint}/received`);
  }

  /**
   * Validate rating before submission
   */
  validateRating(dto: CreateRatingDto): Observable<RatingValidationDto> {
    return this.api.post<RatingValidationDto>(`${this.endpoint}/validate`, dto);
  }

  /**
   * Get rating categories averages for student
   */
  getStudentCategoryAverages(studentId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/student/${studentId}/category-averages`);
  }

  /**
   * Get recent ratings by band
   */
  getRecentRatingsByBand(bandId: number, limit: number = 10): Observable<RatingSummaryDto[]> {
    return this.api.get<RatingSummaryDto[]>(`${this.endpoint}/band/${bandId}/recent`, { limit });
  }

  /**
   * Compare student ratings
   */
  compareStudents(studentIds: number[]): Observable<any> {
    return this.api.post(`${this.endpoint}/compare`, { studentIds });
  }

  /**
   * Get rating trends over time for student
   */
  getStudentRatingTrends(studentId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/student/${studentId}/trends`);
  }

  /**
   * Get top rated students by band
   */
  getTopRatedStudents(bandId: number, limit: number = 10): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/top-students`, { limit });
  }

  /**
   * Get rating distribution for band
   */
  getRatingDistribution(bandId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/distribution`);
  }

  /**
   * Export ratings
   */
  exportRatings(filter?: RatingFilterDto, format: 'csv' | 'excel' = 'csv'): Observable<Blob> {
    return this.api.download(`${this.endpoint}/export`, { ...filter, format });
  }

  /**
   * Flag inappropriate rating
   */
  flagRating(ratingId: number, reason: string): Observable<any> {
    return this.api.post(`${this.endpoint}/${ratingId}/flag`, { reason });
  }

  /**
   * Get flagged ratings (Admin/Director)
   */
  getFlaggedRatings(): Observable<RatingDto[]> {
    return this.api.get<RatingDto[]>(`${this.endpoint}/flagged`);
  }

  /**
   * Resolve flagged rating
   */
  resolveFlaggedRating(ratingId: number, action: 'approve' | 'remove', notes?: string): Observable<any> {
    return this.api.post(`${this.endpoint}/${ratingId}/resolve`, { action, notes });
  }
}