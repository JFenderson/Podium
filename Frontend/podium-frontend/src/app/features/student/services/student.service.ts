import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  StudentDetailsDto,
  UpdateStudentDto,
  StudentSummaryDto,
  InterestDto,
  PagedResult,
  StudentFilterDto,
  StudentDashboardDto,
} from '../../../core/models/student.models';
import { RatingDto, ServiceResult } from '../../../core/models/common.models';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class StudentService {
  private readonly endpoint = 'Students';

  constructor(private api: ApiService) {}

  /**
   * Get all accessible students (with pagination and filtering)
   */
  getStudents(filter?: StudentFilterDto): Observable<PagedResult<StudentDetailsDto>> {
    return this.api.get<PagedResult<StudentDetailsDto>>(this.endpoint, filter);
  }

  /**
   * Get student by ID
   */
  getStudent(id: number): Observable<StudentDetailsDto> {
    return this.api.get<StudentDetailsDto>(`${this.endpoint}/${id}`);
  }

  /**
   * Get current student's profile
   */
  getMyProfile(): Observable<StudentDetailsDto> {
    return this.api.get<StudentDetailsDto>(`${this.endpoint}/me`);
  }

  /**
   * Update student profile
   */
  updateStudent(id: number, dto: UpdateStudentDto): Observable<any> {
    return this.api.put(`${this.endpoint}/${id}`, dto);
  }

  /**
   * Show interest in a band
   */
  showInterest(dto: InterestDto): Observable<any> {
    return this.api.post(`${this.endpoint}/interest`, dto);
  }

  /**
   * Rate a student (BandStaff only)
   */
  rateStudent(studentId: number, rating: RatingDto): Observable<any> {
    return this.api.post(`${this.endpoint}/${studentId}/rate`, rating);
  }

  /**
   * Get student's ratings
   */
  getStudentRatings(studentId: number): Observable<RatingDto[]> {
    return this.api.get<RatingDto[]>(`${this.endpoint}/${studentId}/ratings`);
  }

  /**
   * Search students (for autocomplete, etc.)
   */
  searchStudents(searchTerm: string): Observable<StudentSummaryDto[]> {
    return this.api.get<StudentSummaryDto[]>(`${this.endpoint}/search`, { search: searchTerm });
  }

  /**
   * Upload student video
   */
  uploadVideo(studentId: number, formData: FormData): Observable<any> {
    return this.api.upload(`${this.endpoint}/${studentId}/video`, formData);
  }

  /**
   * Delete student video
   */
  deleteVideo(studentId: number, videoId: number): Observable<any> {
    return this.api.delete(`${this.endpoint}/${studentId}/video/${videoId}`);
  }

getDashboard(): Observable<StudentDashboardDto> {
    return this.api.get<ServiceResult<StudentDashboardDto>>(`${this.endpoint}/dashboard`).pipe(
      map(response => {
        // Check if data is missing despite a successful response
        if (!response.data) {
          throw new Error('Dashboard data is missing');
        }
        return response.data;
      })
    );
  }
}
