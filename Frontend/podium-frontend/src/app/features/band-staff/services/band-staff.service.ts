import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  BandStaffDto,
  CreateBandStaffDto,
  UpdateBandStaffDto,
  BandStaffPermissionsDto,
  BandStaffSummaryDto
} from '../../../core/models/band-staff.models';

@Injectable({
  providedIn: 'root'
})
export class BandStaffService {
  private readonly endpoint = 'BandStaff';

  constructor(private api: ApiService) {}

  /**
   * Get all band staff (Directors only)
   */
  getAllStaff(): Observable<BandStaffDto[]> {
    return this.api.get<BandStaffDto[]>(this.endpoint);
  }

  /**
   * Get band staff by ID
   */
  getStaffById(id: number): Observable<BandStaffDto> {
    return this.api.get<BandStaffDto>(`${this.endpoint}/${id}`);
  }

  /**
   * Get staff by band ID
   */
  getStaffByBand(bandId: number): Observable<BandStaffDto[]> {
    return this.api.get<BandStaffDto[]>(`${this.endpoint}/band/${bandId}`);
  }

  /**
   * Create new band staff member (Directors only)
   */
  createStaff(dto: CreateBandStaffDto): Observable<BandStaffDto> {
    return this.api.post<BandStaffDto>(this.endpoint, dto);
  }

  /**
   * Update band staff member (Directors only)
   */
  updateStaff(id: number, dto: UpdateBandStaffDto): Observable<any> {
    return this.api.put(`${this.endpoint}/${id}`, dto);
  }

  /**
   * Delete band staff member (Directors only)
   */
  deleteStaff(id: number): Observable<any> {
    return this.api.delete(`${this.endpoint}/${id}`);
  }

  /**
   * Get current staff member's profile
   */
  getMyProfile(): Observable<BandStaffDto> {
    return this.api.get<BandStaffDto>(`${this.endpoint}/me`);
  }

  /**
   * Get current staff member's permissions
   */
  getMyPermissions(): Observable<BandStaffPermissionsDto> {
    return this.api.get<BandStaffPermissionsDto>(`${this.endpoint}/me/permissions`);
  }

  /**
   * Update staff permissions (Directors only)
   */
  updatePermissions(id: number, permissions: BandStaffPermissionsDto): Observable<any> {
    return this.api.put(`${this.endpoint}/${id}/permissions`, permissions);
  }

  /**
   * Search staff members
   */
  searchStaff(searchTerm: string, bandId?: number): Observable<BandStaffSummaryDto[]> {
    return this.api.get<BandStaffSummaryDto[]>(`${this.endpoint}/search`, {
      search: searchTerm,
      bandId
    });
  }

  /**
   * Get staff statistics
   */
  getStaffStats(staffId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/${staffId}/stats`);
  }

  /**
   * Invite new staff member via email
   */
  inviteStaff(email: string, bandId: number, role: string): Observable<any> {
    return this.api.post(`${this.endpoint}/invite`, { email, bandId, role });
  }

  /**
   * Deactivate staff member
   */
  deactivateStaff(id: number): Observable<any> {
    return this.api.post(`${this.endpoint}/${id}/deactivate`, {});
  }

  /**
   * Reactivate staff member
   */
  reactivateStaff(id: number): Observable<any> {
    return this.api.post(`${this.endpoint}/${id}/reactivate`, {});
  }
}