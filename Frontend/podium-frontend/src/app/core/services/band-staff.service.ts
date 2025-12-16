import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BandStaffDto } from '../models/band-staff.models';

@Injectable({ providedIn: 'root' })
export class BandStaffService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/bandstaff`;

  getMyInfo(): Observable<BandStaffDto> {
    return this.http.get<BandStaffDto>(`${this.apiUrl}/me`);
  }
}