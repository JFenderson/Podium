import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  ScholarshipOfferDto,
  CreateScholarshipOfferDto,
  UpdateScholarshipOfferDto,
  RespondToScholarshipOfferDto,
  GuardianApprovalDto,
  ScholarshipFilterDto,
  ScholarshipSummaryDto,
  OfferSummary
} from '../../../core/models/scholarship.models';
import { PagedResult } from '../../../core/models/student.models';
import { HttpParams } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ScholarshipService {
  private readonly endpoint = 'ScholarshipOffers';

  constructor(private api: ApiService) {}

  /**
   * Get scholarship offers (filtered)
   */
  getOffers(filter?: ScholarshipFilterDto): Observable<PagedResult<ScholarshipOfferDto>> {
    return this.api.get<PagedResult<ScholarshipOfferDto>>(this.endpoint, filter);
  }

  /**
   * Get specific offer by ID
   */
  getOffer(id: number): Observable<ScholarshipOfferDto> {
    return this.api.get<ScholarshipOfferDto>(`${this.endpoint}/${id}`);
  }

  /**
   * Get offers for current student
   */
  getMyOffers(): Observable<ScholarshipOfferDto[]> {
    return this.api.get<ScholarshipOfferDto[]>(`${this.endpoint}/my-offers`);
  }

  /**
   * Create new scholarship offer (BandStaff only)
   */
  createOffer(dto: CreateScholarshipOfferDto): Observable<ScholarshipOfferDto> {
    return this.api.post<ScholarshipOfferDto>(this.endpoint, dto);
  }

  /**
   * Update scholarship offer (BandStaff only)
   */
  updateOffer(id: number, dto: UpdateScholarshipOfferDto): Observable<any> {
    return this.api.put(`${this.endpoint}/${id}`, dto);
  }

  /**
   * Send offer to student (change from Draft to Sent)
   */
  sendOffer(id: number): Observable<any> {
    return this.api.post(`${this.endpoint}/${id}/send`, {});
  }

  /**
   * Respond to offer (Student only)
   */
  respondToOffer(id: number, dto: RespondToScholarshipOfferDto): Observable<any> {
    return this.api.put(`${this.endpoint}/${id}/respond`, dto);
  }

  /**
   * Guardian approval for offer
   */
  guardianApproval(id: number, dto: GuardianApprovalDto): Observable<any> {
    return this.api.post(`${this.endpoint}/${id}/guardian-approval`, dto);
  }

  /**
   * Withdraw offer (BandStaff only)
   */
  withdrawOffer(id: number, reason?: string): Observable<any> {
    return this.api.post(`${this.endpoint}/${id}/withdraw`, { reason });
  }

  /**
   * Delete offer (BandStaff only, Draft status only)
   */
  deleteOffer(id: number): Observable<any> {
    return this.api.delete(`${this.endpoint}/${id}`);
  }

  /**
   * Get offers pending guardian approval (Guardian only)
   */
  getPendingApprovals(): Observable<ScholarshipOfferDto[]> {
    return this.api.get<ScholarshipOfferDto[]>(`${this.endpoint}/pending-approvals`);
  }

  /**
   * Get offer statistics for band
   */
  getOfferStats(bandId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/stats`, { bandId });
  }

  rescindOffer(id: number, reason: string): Observable<any> {
    return this.api.put(`${this.endpoint}/${id}/rescind`, { reason });
  }

  getStudentOfferSummaries(studentId: number, page: number = 1, pageSize: number = 10): Observable<PagedResult<OfferSummary>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.api.get<PagedResult<OfferSummary>>(`${this.endpoint}/student/${studentId}/summaries`, { params });
  }
  
}