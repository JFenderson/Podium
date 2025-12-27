import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, tap, catchError } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import {
  StudentSearchFilters,
  StudentSearchResponse,
  SavedSearch,
  SearchSuggestion,
} from '../../../core/models/student-search.models';

@Injectable({
  providedIn: 'root',
})
export class StudentSearchService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Students`;

  // Local storage keys
  private readonly SAVED_SEARCHES_KEY = 'podium_saved_searches';
  private readonly RECENT_FILTERS_KEY = 'podium_recent_filters';
  private readonly WATCHLIST_KEY = 'podium_watchlist';

  // Observable for filter changes
  private filtersSubject = new BehaviorSubject<StudentSearchFilters>({});
  public filters$ = this.filtersSubject.asObservable();

  constructor() {}

  /**
   * Search students with filters
   */
  searchStudents(filters: StudentSearchFilters): Observable<StudentSearchResponse> {
    let params = new HttpParams();

    // Build query parameters
    if (filters.searchTerm) {
      params = params.set('searchTerm', filters.searchTerm);
    }

    if (filters.instruments?.length) {
      filters.instruments.forEach((inst) => {
        params = params.append('instruments', inst);
      });
    }

    if (filters.states?.length) {
      filters.states.forEach((state) => {
        params = params.append('states', state);
      });
    }

    if (filters.isHBCU !== undefined) {
      params = params.set('isHBCU', filters.isHBCU.toString());
    }

    if (filters.distance) {
      params = params.set('distance', filters.distance.toString());
    }

    if (filters.zipCode) {
      params = params.set('zipCode', filters.zipCode);
    }

    if (filters.minGPA !== undefined) {
      params = params.set('minGPA', filters.minGPA.toString());
    }

    if (filters.maxGPA !== undefined) {
      params = params.set('maxGPA', filters.maxGPA.toString());
    }

    if (filters.graduationYears?.length) {
      filters.graduationYears.forEach((year) => {
        params = params.append('graduationYears', year.toString());
      });
    }

    if (filters.majors?.length) {
      filters.majors.forEach((major) => {
        params = params.append('majors', major);
      });
    }

    if (filters.skillLevels?.length) {
      filters.skillLevels.forEach((level) => {
        params = params.append('skillLevels', level);
      });
    }

    if (filters.minYearsExperience !== undefined) {
      params = params.set('minYearsExperience', filters.minYearsExperience.toString());
    }

    if (filters.maxYearsExperience !== undefined) {
      params = params.set('maxYearsExperience', filters.maxYearsExperience.toString());
    }

    if (filters.hasVideo !== undefined) {
      params = params.set('hasVideo', filters.hasVideo.toString());
    }

    if (filters.hasAuditionVideo !== undefined) {
      params = params.set('hasAuditionVideo', filters.hasAuditionVideo.toString());
    }

    if (filters.isAvailable !== undefined) {
      params = params.set('isAvailable', filters.isAvailable.toString());
    }

    if (filters.isActivelyRecruiting !== undefined) {
      params = params.set('isActivelyRecruiting', filters.isActivelyRecruiting.toString());
    }

    if (filters.hasScholarshipOffers !== undefined) {
      params = params.set('hasScholarshipOffers', filters.hasScholarshipOffers.toString());
    }

    if (filters.lastActivityDays) {
      params = params.set('lastActivityDays', filters.lastActivityDays.toString());
    }

    if (filters.sortBy) {
      params = params.set('sortBy', filters.sortBy);
    }

    if (filters.sortDirection) {
      params = params.set('sortDirection', filters.sortDirection);
    }

    if (filters.newFilter) {
      params = params.set('newFilter', filters.newFilter);
    }

    params = params.set('page', (filters.page || 1).toString());
    params = params.set('pageSize', (filters.pageSize || 20).toString());

    return this.http.get<StudentSearchResponse>(`${this.apiUrl}/search`, { params }).pipe(
      tap((response) => {
        // Save to recent filters
        this.saveRecentFilters(filters);
      }),
      catchError((error) => {
        console.error('Search error:', error);
        // Return empty results on error
        return of({
          results: [],
          totalCount: 0,
          page: 1,
          pageSize: 20,
          totalPages: 0,
          filters,
          appliedFiltersCount: 0,
        });
      })
    );
  }

  /**
   * Get autocomplete suggestions
   */
  getSearchSuggestions(term: string): Observable<SearchSuggestion[]> {
    if (!term || term.length < 2) {
      return of([]);
    }

    const params = new HttpParams().set('term', term).set('limit', '10');

    return this.http
      .get<SearchSuggestion[]>(`${this.apiUrl}/search/suggestions`, { params })
      .pipe(catchError(() => of([])));
  }

  /**
   * Debounced search
   */
  createDebouncedSearch(
    searchTerm$: Observable<string>,
    filters: StudentSearchFilters
  ): Observable<StudentSearchResponse> {
    return searchTerm$.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      switchMap((term) => this.searchStudents({ ...filters, searchTerm: term }))
    );
  }

  /**
   * Update filters
   */
  updateFilters(filters: StudentSearchFilters): void {
    this.filtersSubject.next(filters);
  }

  /**
   * Get current filters
   */
  getCurrentFilters(): StudentSearchFilters {
    return this.filtersSubject.value;
  }

  // ============================================
  // SAVED SEARCHES
  // ============================================

  /**
   * Save a search
   */
  saveSearch(name: string, filters: StudentSearchFilters): SavedSearch {
    const searches = this.getSavedSearches();
    const newSearch: SavedSearch = {
      id: Date.now(),
      name,
      filters,
      createdDate: new Date(),
      lastUsedDate: new Date(),
    };

    searches.push(newSearch);
    localStorage.setItem(this.SAVED_SEARCHES_KEY, JSON.stringify(searches));

    return newSearch;
  }

  /**
   * Get all saved searches
   */
  getSavedSearches(): SavedSearch[] {
    const saved = localStorage.getItem(this.SAVED_SEARCHES_KEY);
    if (!saved) return [];

    try {
      const searches = JSON.parse(saved);
      // Convert date strings back to Date objects
      return searches.map((s: any) => ({
        ...s,
        createdDate: new Date(s.createdDate),
        lastUsedDate: s.lastUsedDate ? new Date(s.lastUsedDate) : undefined,
      }));
    } catch {
      return [];
    }
  }

  /**
   * Delete a saved search
   */
  deleteSavedSearch(id: number): void {
    const searches = this.getSavedSearches().filter((s) => s.id !== id);
    localStorage.setItem(this.SAVED_SEARCHES_KEY, JSON.stringify(searches));
  }

  /**
   * Update last used date
   */
  updateSearchLastUsed(id: number): void {
    const searches = this.getSavedSearches();
    const search = searches.find((s) => s.id === id);
    if (search) {
      search.lastUsedDate = new Date();
      localStorage.setItem(this.SAVED_SEARCHES_KEY, JSON.stringify(searches));
    }
  }

  // ============================================
  // WATCHLIST / FAVORITES
  // ============================================

  /**
   * Add student to watchlist
   */
  addToWatchlist(studentId: number): void {
    const watchlist = this.getWatchlist();
    if (!watchlist.includes(studentId)) {
      watchlist.push(studentId);
      localStorage.setItem(this.WATCHLIST_KEY, JSON.stringify(watchlist));
    }
  }

  /**
   * Remove from watchlist
   */
  removeFromWatchlist(studentId: number): void {
    const watchlist = this.getWatchlist().filter((id) => id !== studentId);
    localStorage.setItem(this.WATCHLIST_KEY, JSON.stringify(watchlist));
  }

  /**
   * Check if student is watchlisted
   */
  isWatchlisted(studentId: number): boolean {
    return this.getWatchlist().includes(studentId);
  }

  /**
   * Get watchlist
   */
  getWatchlist(): number[] {
    const saved = localStorage.getItem(this.WATCHLIST_KEY);
    return saved ? JSON.parse(saved) : [];
  }

  /**
   * Toggle watchlist
   */
  toggleWatchlist(studentId: number): boolean {
    const isWatchlisted = this.isWatchlisted(studentId);
    if (isWatchlisted) {
      this.removeFromWatchlist(studentId);
    } else {
      this.addToWatchlist(studentId);
    }
    return !isWatchlisted;
  }

  // ============================================
  // RECENT FILTERS
  // ============================================

  /**
   * Save filters to recent
   */
  private saveRecentFilters(filters: StudentSearchFilters): void {
    const recent = this.getRecentFilters();

    // Don't save if it's the same as the last one
    if (recent.length > 0 && JSON.stringify(recent[0]) === JSON.stringify(filters)) {
      return;
    }

    recent.unshift(filters);

    // Keep only last 5
    const trimmed = recent.slice(0, 5);
    localStorage.setItem(this.RECENT_FILTERS_KEY, JSON.stringify(trimmed));
  }

  /**
   * Get recent filter sets
   */
  getRecentFilters(): StudentSearchFilters[] {
    const saved = localStorage.getItem(this.RECENT_FILTERS_KEY);
    return saved ? JSON.parse(saved) : [];
  }

  /**
   * Clear recent filters
   */
  clearRecentFilters(): void {
    localStorage.removeItem(this.RECENT_FILTERS_KEY);
  }

  // ============================================
  // UTILITY
  // ============================================

  /**
   * Count active filters
   */
  countActiveFilters(filters: StudentSearchFilters): number {
    let count = 0;

    if (filters.searchTerm) count++;
    if (filters.instruments?.length) count++;
    if (filters.states?.length) count++;
    if (filters.isHBCU) count++;
    if (filters.distance) count++;
    if (filters.minGPA !== undefined || filters.maxGPA !== undefined) count++;
    if (filters.graduationYears?.length) count++;
    if (filters.majors?.length) count++;
    if (filters.skillLevels?.length) count++;
    if (filters.minYearsExperience !== undefined || filters.maxYearsExperience !== undefined)
      count++;
    if (filters.hasVideo) count++;
    if (filters.hasAuditionVideo) count++;
    if (filters.isAvailable) count++;
    if (filters.isActivelyRecruiting) count++;
    if (filters.lastActivityDays) count++;

    return count;
  }

  /**
   * Clear all filters
   */
  clearAllFilters(): StudentSearchFilters {
    const cleared: StudentSearchFilters = {
      page: 1,
      pageSize: 20,
      sortBy: 'relevance',
      sortDirection: 'desc',
    };
    this.filtersSubject.next(cleared);
    return cleared;
  }
}
