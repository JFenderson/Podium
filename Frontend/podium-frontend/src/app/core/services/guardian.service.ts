import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GuardianDashboardDto } from '../models/guardian.models';

@Injectable({ providedIn: 'root' })
export class GuardianService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/guardian`;

  getDashboard(): Observable<GuardianDashboardDto> {
    return this.http.get<GuardianDashboardDto>(`${this.apiUrl}/dashboard`);
  }
}