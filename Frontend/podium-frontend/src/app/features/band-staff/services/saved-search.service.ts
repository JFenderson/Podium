import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import {
  SavedSearchDto,
  CreateSavedSearchDto,
  UpdateSavedSearchDto,
  SavedSearchSummary,
  ShareSearchDto,
  StudentSearchRequest,
  StudentSearchResponse,
  SearchResultCountDto,
  SearchFilterCriteria
} from '../../../core/models/saved-search.models';

@Injectable({
  providedIn: 'root'
})
export class SavedSearchService {
  private readonly apiUrl = `${environment.apiUrl}/recruiters/saved-searches`;

  constructor(private http: HttpClient) {}

  /**
   * Get all saved searches for current recruiter
   */
  getSavedSearches(): Observable<SavedSearchSummary[]> {
    return this.http.get<SavedSearchSummary[]>(this.apiUrl);
  }

  /**
   * Get a specific saved search by ID
   */
  getSavedSearch(id: number): Observable<SavedSearchDto> {
    return this.http.get<SavedSearchDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create a new saved search
   */
  createSavedSearch(dto: CreateSavedSearchDto): Observable<SavedSearchDto> {
    return this.http.post<SavedSearchDto>(this.apiUrl, dto);
  }

  /**
   * Update an existing saved search
   */
  updateSavedSearch(id: number, dto: UpdateSavedSearchDto): Observable<SavedSearchDto> {
    return this.http.put<SavedSearchDto>(`${this.apiUrl}/${id}`, dto);
  }

  /**
   * Delete a saved search
   */
  deleteSavedSearch(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Mark a saved search as used
   */
  markSearchUsed(id: number): Observable<SavedSearchDto> {
    return this.http.post<SavedSearchDto>(`${this.apiUrl}/${id}/mark-used`, {});
  }

  /**
   * Execute a search with filters
   */
  executeSearch(request: StudentSearchRequest): Observable<StudentSearchResponse> {
    return this.http.post<StudentSearchResponse>(`${this.apiUrl}/search`, request);
  }

  /**
   * Execute a saved search
   */
  executeSavedSearch(
    savedSearchId: number, 
    page: number = 1, 
    pageSize: number = 20
  ): Observable<StudentSearchResponse> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<StudentSearchResponse>(
      `${this.apiUrl}/${savedSearchId}/execute`,
      { params }
    );
  }

  /**
   * Get result count for filters (debounced on backend)
   */
  getSearchResultCount(filters: SearchFilterCriteria): Observable<SearchResultCountDto> {
    return this.http.post<SearchResultCountDto>(`${this.apiUrl}/count`, filters);
  }

  /**
   * Share a saved search
   */
  shareSearch(id: number): Observable<ShareSearchDto> {
    return this.http.post<ShareSearchDto>(`${this.apiUrl}/${id}/share`, {});
  }

  /**
   * Unshare a saved search
   */
  unshareSearch(id: number): Observable<ShareSearchDto> {
    return this.http.post<ShareSearchDto>(`${this.apiUrl}/${id}/unshare`, {});
  }

  /**
   * Get shared search results (public endpoint)
   */
  getSharedSearchResults(
    shareToken: string, 
    page: number = 1, 
    pageSize: number = 20
  ): Observable<StudentSearchResponse> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<StudentSearchResponse>(
      `${environment.apiUrl}/recruiters/saved-searches/shared/${shareToken}`,
      { params }
    );
  }

  /**
   * Get search templates
   */
  getSearchTemplates(): Observable<SavedSearchSummary[]> {
    return this.http.get<SavedSearchSummary[]>(`${this.apiUrl}/templates`);
  }
}