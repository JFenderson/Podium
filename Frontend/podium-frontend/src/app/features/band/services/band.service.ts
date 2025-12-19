import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  BandSummaryDto,
  BandDetailDto,
  BandFilterDto,
  BandStatsDto
} from '../../../core/models/band.models';

@Injectable({
  providedIn: 'root'
})
export class BandService {
  private readonly endpoint = 'Band';

  constructor(private api: ApiService) {}

  /**
   * Get all active bands
   */
  getBands(filter?: BandFilterDto): Observable<BandSummaryDto[]> {
    return this.api.get<BandSummaryDto[]>(this.endpoint, filter);
  }

  /**
   * Get band details by ID
   */
  getBand(id: number): Observable<BandDetailDto> {
    return this.api.get<BandDetailDto>(`${this.endpoint}/${id}`);
  }

  /**
   * Search bands (for autocomplete)
   */
  searchBands(searchTerm: string): Observable<BandSummaryDto[]> {
    return this.api.get<BandSummaryDto[]>(`${this.endpoint}/search`, { search: searchTerm });
  }

  /**
   * Get band statistics
   */
  getBandStats(bandId: number): Observable<BandStatsDto> {
    return this.api.get<BandStatsDto>(`${this.endpoint}/${bandId}/stats`);
  }

  /**
   * Get states with active bands
   */
  getStatesWithBands(): Observable<string[]> {
    return this.api.get<string[]>(`${this.endpoint}/states`);
  }

  /**
   * Get cities with active bands in a state
   */
  getCitiesInState(state: string): Observable<string[]> {
    return this.api.get<string[]>(`${this.endpoint}/cities`, { state });
  }
}