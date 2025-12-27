// student-search.component.ts
// Frontend/podium-frontend/src/app/features/recruiter/components/student-search/student-search.component.ts

import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { StudentSearchService } from '../../services/student-search.service';
import {
  StudentSearchFilters,
  StudentSearchResultDto,
  SavedSearch,
  QuickFilterChip,
  SearchSuggestion,
  SKILL_LEVELS,
  INSTRUMENTS,
  US_STATES,
  GRADUATION_YEARS,
  COMMON_MAJORS
} from '../../../../core/models/student-search.models';

@Component({
  selector: 'app-student-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './student-search.component.html',
  styleUrls: ['./student-search.component.scss']
})
export class StudentSearchComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private searchService = inject(StudentSearchService);
  private destroy$ = new Subject<void>();

  // UI State
  showAdvancedFilters = signal(false);
  showSavedSearches = signal(false);
  isLoading = signal(false);
  isSidebarCollapsed = signal(false);
  
  // Search Results
  searchResults = signal<StudentSearchResultDto[]>([]);
  totalResults = signal(0);
  currentPage = signal(1);
  pageSize = signal(20);
  totalPages = computed(() => Math.ceil(this.totalResults() / this.pageSize()));
  
  // Filters
  currentFilters = signal<StudentSearchFilters>({});
  appliedFiltersCount = computed(() => 
    this.searchService.countActiveFilters(this.currentFilters())
  );
  
  // Saved Searches
  savedSearches = signal<SavedSearch[]>([]);
  
  // Autocomplete
  searchSuggestions = signal<SearchSuggestion[]>([]);
  showSuggestions = signal(false);
  
  // Forms
  searchForm!: FormGroup;
  filterForm!: FormGroup;
  
  // Constants for templates
  readonly SKILL_LEVELS = SKILL_LEVELS;
  readonly INSTRUMENTS = INSTRUMENTS;
  readonly US_STATES = US_STATES;
  readonly GRADUATION_YEARS = GRADUATION_YEARS;
  readonly COMMON_MAJORS = COMMON_MAJORS;
  
  // Filter panel state
  expandedPanels = signal<Set<string>>(new Set(['instruments', 'location']));
  
  // Quick filter chips
  quickFilterChips = computed<QuickFilterChip[]>(() => {
    const filters = this.currentFilters();
    const chips: QuickFilterChip[] = [];
    
    if (filters.instruments?.length) {
      filters.instruments.forEach(inst => {
        chips.push({
          label: inst,
          filterKey: 'instruments',
          value: inst,
          removable: true
        });
      });
    }
    
    if (filters.states?.length) {
      filters.states.forEach(state => {
        const stateName = US_STATES.find(s => s.code === state)?.name || state;
        chips.push({
          label: stateName,
          filterKey: 'states',
          value: state,
          removable: true
        });
      });
    }
    
    if (filters.isHBCU) {
      chips.push({
        label: 'HBCU Only',
        filterKey: 'isHBCU',
        value: true,
        removable: true
      });
    }
    
    if (filters.minGPA !== undefined || filters.maxGPA !== undefined) {
      const min = filters.minGPA || 0;
      const max = filters.maxGPA || 4.0;
      chips.push({
        label: `GPA: ${min.toFixed(1)} - ${max.toFixed(1)}`,
        filterKey: 'gpa',
        value: { min, max },
        removable: true
      });
    }
    
    if (filters.graduationYears?.length) {
      filters.graduationYears.forEach(year => {
        chips.push({
          label: `Class of ${year}`,
          filterKey: 'graduationYears',
          value: year,
          removable: true
        });
      });
    }
    
    if (filters.skillLevels?.length) {
      filters.skillLevels.forEach(level => {
        chips.push({
          label: level,
          filterKey: 'skillLevels',
          value: level,
          removable: true
        });
      });
    }
    
    if (filters.hasVideo) {
      chips.push({
        label: 'Has Video',
        filterKey: 'hasVideo',
        value: true,
        removable: true
      });
    }
    
    return chips;
  });
  
  // Sort options
  sortOptions = [
    { value: 'relevance', label: 'Most Relevant' },
    { value: 'gpa', label: 'Highest GPA' },
    { value: 'experience', label: 'Most Experienced' },
    { value: 'recent', label: 'Recently Active' },
    { value: 'rating', label: 'Highest Rated' },
    { value: 'name', label: 'Name (A-Z)' }
  ];

  ngOnInit(): void {
    this.initForms();
    this.loadSavedSearches();
    this.setupSearchDebounce();
    this.loadFiltersFromUrl();
    this.performSearch();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initForms(): void {
    // Main search form
    this.searchForm = this.fb.group({
      searchTerm: ['']
    });

    // Advanced filters form
    this.filterForm = this.fb.group({
      // Instruments
      instruments: [[]],
      
      // Location
      states: [[]],
      isHBCU: [false],
      distance: [null],
      zipCode: [''],
      
      // Academics
      minGPA: [null],
      maxGPA: [null],
      graduationYears: [[]],
      majors: [[]],
      
      // Experience
      skillLevels: [[]],
      minYearsExperience: [null],
      maxYearsExperience: [null],
      hasVideo: [false],
      hasAuditionVideo: [false],
      
      // Engagement
      isAvailable: [false],
      isActivelyRecruiting: [false],
      hasScholarshipOffers: [false],
      lastActivityDays: [null],
      
      // Sort
      sortBy: ['relevance'],
      sortDirection: ['desc'],

      newFilter: ['']
    });
  }

  private setupSearchDebounce(): void {
    this.searchForm.get('searchTerm')?.valueChanges
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(term => {
        this.updateSearchSuggestions(term);
        this.performSearch();
      });
  }

  private updateSearchSuggestions(term: string): void {
    if (!term || term.length < 2) {
      this.searchSuggestions.set([]);
      this.showSuggestions.set(false);
      return;
    }

    this.searchService.getSearchSuggestions(term).subscribe({
      next: suggestions => {
        this.searchSuggestions.set(suggestions);
        this.showSuggestions.set(suggestions.length > 0);
      }
    });
  }

  performSearch(): void {
    this.isLoading.set(true);
    
    const filters: StudentSearchFilters = {
      ...this.filterForm.value,
      searchTerm: this.searchForm.value.searchTerm,
      page: this.currentPage(),
      pageSize: this.pageSize()
    };

    this.currentFilters.set(filters);
    this.updateUrlParams(filters);

    this.searchService.searchStudents(filters).subscribe({
      next: response => {
        this.searchResults.set(response.results);
        this.totalResults.set(response.totalCount);
        this.isLoading.set(false);
      },
      error: error => {
        console.error('Search failed:', error);
        this.isLoading.set(false);
      }
    });
  }

  // ============================================
  // FILTER MANAGEMENT
  // ============================================

  toggleFilterPanel(panelName: string): void {
    const expanded = this.expandedPanels();
    if (expanded.has(panelName)) {
      expanded.delete(panelName);
    } else {
      expanded.add(panelName);
    }
    this.expandedPanels.set(new Set(expanded));
  }

 toggleInstrument(instrument: string, checked: boolean): void {
    const current = this.filterForm.value.instruments || [];
    if (checked) {
      this.filterForm.patchValue({ instruments: [...current, instrument] });
    } else {
      this.filterForm.patchValue({ instruments: current.filter((i: string) => i !== instrument) });
    }
  }

  toggleState(stateCode: string, checked: boolean): void {
    const current = this.filterForm.value.states || [];
    if (checked) {
      this.filterForm.patchValue({ states: [...current, stateCode] });
    } else {
      this.filterForm.patchValue({ states: current.filter((s: string) => s !== stateCode) });
    }
  }

  toggleGraduationYear(year: number, checked: boolean): void {
    const current = this.filterForm.value.graduationYears || [];
    if (checked) {
      this.filterForm.patchValue({ graduationYears: [...current, year] });
    } else {
      this.filterForm.patchValue({ graduationYears: current.filter((y: number) => y !== year) });
    }
  }

  toggleSkillLevel(level: string, checked: boolean): void {
    const current = this.filterForm.value.skillLevels || [];
    if (checked) {
      this.filterForm.patchValue({ skillLevels: [...current, level] });
    } else {
      this.filterForm.patchValue({ skillLevels: current.filter((l: string) => l !== level) });
    }
  }

  isPanelExpanded(panelName: string): boolean {
    return this.expandedPanels().has(panelName);
  }
  
    isInstrumentSelected(instrument: string): boolean {
    return (this.filterForm.value.instruments || []).includes(instrument);
  }

  isStateSelected(stateCode: string): boolean {
    return (this.filterForm.value.states || []).includes(stateCode);
  }

  isGraduationYearSelected(year: number): boolean {
    return (this.filterForm.value.graduationYears || []).includes(year);
  }

  isSkillLevelSelected(level: string): boolean {
    return (this.filterForm.value.skillLevels || []).includes(level);
  }

  removeFilterChip(chip: QuickFilterChip): void {
    const currentValue = this.filterForm.get(chip.filterKey)?.value;
    
    if (Array.isArray(currentValue)) {
      const updated = currentValue.filter(v => v !== chip.value);
      this.filterForm.get(chip.filterKey)?.setValue(updated);
    } else if (chip.filterKey === 'gpa') {
      this.filterForm.patchValue({ minGPA: null, maxGPA: null });
    } else {
      this.filterForm.get(chip.filterKey)?.setValue(false);
    }
    
    this.performSearch();
  }

  clearAllFilters(): void {
    this.filterForm.reset({
      instruments: [],
      states: [],
      isHBCU: false,
      graduationYears: [],
      majors: [],
      skillLevels: [],
      hasVideo: false,
      hasAuditionVideo: false,
      isAvailable: false,
      isActivelyRecruiting: false,
      hasScholarshipOffers: false,
      sortBy: 'relevance',
      sortDirection: 'desc'
    });
    this.searchForm.patchValue({ searchTerm: '' });
    this.currentPage.set(1);
    this.performSearch();
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.performSearch();
  }

  // ============================================
  // SAVED SEARCHES
  // ============================================

  loadSavedSearches(): void {
    this.savedSearches.set(this.searchService.getSavedSearches());
  }

  saveCurrentSearch(): void {
    const name = prompt('Enter a name for this search:');
    if (!name) return;

    const filters = this.currentFilters();
    this.searchService.saveSearch(name, filters);
    this.loadSavedSearches();
  }

  loadSavedSearch(search: SavedSearch): void {
    this.filterForm.patchValue(search.filters);
    if (search.filters.searchTerm) {
      this.searchForm.patchValue({ searchTerm: search.filters.searchTerm });
    }
    this.searchService.updateSearchLastUsed(search.id);
    this.showSavedSearches.set(false);
    this.performSearch();
  }

  deleteSavedSearch(search: SavedSearch, event: Event): void {
    event.stopPropagation();
    if (confirm(`Delete saved search "${search.name}"?`)) {
      this.searchService.deleteSavedSearch(search.id);
      this.loadSavedSearches();
    }
  }

  // ============================================
  // WATCHLIST
  // ============================================

  toggleWatchlist(student: StudentSearchResultDto, event: Event): void {
    event.stopPropagation();
    const isWatchlisted = this.searchService.toggleWatchlist(student.studentId);
    
    // Update the result in place
    const results = this.searchResults();
    const index = results.findIndex(s => s.studentId === student.studentId);
    if (index !== -1) {
      results[index] = { ...student, isWatchlisted };
      this.searchResults.set([...results]);
    }
  }

  isStudentWatchlisted(studentId: number): boolean {
    return this.searchService.isWatchlisted(studentId);
  }

  // ============================================
  // PAGINATION & SORTING
  // ============================================

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.performSearch();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  changeSort(sortBy: string): void {
    const current = this.filterForm.value.sortBy;
    const currentDir = this.filterForm.value.sortDirection;
    
    if (current === sortBy) {
      // Toggle direction
      this.filterForm.patchValue({
        sortDirection: currentDir === 'asc' ? 'desc' : 'asc'
      });
    } else {
      this.filterForm.patchValue({
        sortBy,
        sortDirection: 'desc'
      });
    }
    
    this.performSearch();
  }

  // ============================================
  // URL SYNC
  // ============================================

  private updateUrlParams(filters: StudentSearchFilters): void {
    const queryParams: any = {};
    
    if (filters.searchTerm) queryParams.q = filters.searchTerm;
    if (filters.instruments?.length) queryParams.instruments = filters.instruments.join(',');
    if (filters.states?.length) queryParams.states = filters.states.join(',');
    if (filters.minGPA) queryParams.minGPA = filters.minGPA;
    if (filters.maxGPA) queryParams.maxGPA = filters.maxGPA;
    if (filters.sortBy) queryParams.sort = filters.sortBy;
    if (filters.page && filters.page > 1) queryParams.page = filters.page;
    
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge'
    });
  }

  private loadFiltersFromUrl(): void {
    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe(params => {
      if (params['q']) {
        this.searchForm.patchValue({ searchTerm: params['q'] });
      }
      
      if (params['instruments']) {
        this.filterForm.patchValue({
          instruments: params['instruments'].split(',')
        });
      }
      
      if (params['states']) {
        this.filterForm.patchValue({
          states: params['states'].split(',')
        });
      }
      
      if (params['minGPA']) {
        this.filterForm.patchValue({ minGPA: parseFloat(params['minGPA']) });
      }
      
      if (params['maxGPA']) {
        this.filterForm.patchValue({ maxGPA: parseFloat(params['maxGPA']) });
      }
      
      if (params['sort']) {
        this.filterForm.patchValue({ sortBy: params['sort'] });
      }
      
      if (params['page']) {
        this.currentPage.set(parseInt(params['page'], 10));
      }
    });
  }

  // ============================================
  // ACTIONS
  // ============================================

  viewStudentProfile(student: StudentSearchResultDto): void {
    this.router.navigate(['/recruiter/student', student.studentId]);
  }

  sendMessage(student: StudentSearchResultDto, event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/recruiter/messages'], {
      queryParams: { studentId: student.studentId }
    });
  }

  sendOffer(student: StudentSearchResultDto, event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/recruiter/offers/create'], {
      queryParams: { studentId: student.studentId }
    });
  }

  requestContact(student: StudentSearchResultDto, event: Event): void {
    event.stopPropagation();
    // Would open a modal or navigate to contact request form
    console.log('Request contact for:', student.fullName);
  }
}