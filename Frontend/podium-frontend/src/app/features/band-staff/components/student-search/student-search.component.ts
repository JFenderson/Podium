// student-search.component.ts
// Frontend/podium-frontend/src/app/features/band-staff/components/student-search/student-search.component.ts

import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { StudentSearchService } from '../../services/student-search.service';
import { SavedSearchService } from '../../services/saved-search.service';
import { FilterConverterService } from '../../services/filter-converter.service';
import {
  StudentSearchFilters,
  StudentSearchResultDto,
  QuickFilterChip,
  SearchSuggestion,
  SKILL_LEVELS,
  INSTRUMENTS,
  US_STATES,
  GRADUATION_YEARS,
  COMMON_MAJORS
} from '../../../../core/models/student-search.models';
import { SearchFilterCriteria } from '../../../../core/models/saved-search.models';

// Import new components
import { SaveSearchModalComponent } from '../save-search-modal/save-search-modal.component';
import { SavedSearchesDropdownComponent } from '../saved-searches-dropdown/saved-searches-dropdown.component';

@Component({
  selector: 'app-student-search',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule,
    SaveSearchModalComponent,
    SavedSearchesDropdownComponent
  ],
  templateUrl: './student-search.component.html',
  styleUrls: ['./student-search.component.scss']
})
export class StudentSearchComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private searchService = inject(StudentSearchService);
  private savedSearchService = inject(SavedSearchService);
  private filterConverter = inject(FilterConverterService);
  private destroy$ = new Subject<void>();

  // UI State
  showAdvancedFilters = signal(false);
  isLoading = signal(false);
  isSidebarCollapsed = signal(false);
  
  // NEW: Save Search Modal State
  isSaveModalOpen = signal(false);
  
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
    
    if (filters.minGPA !== undefined || filters.maxGPA !== undefined) {
      const min = filters.minGPA?.toFixed(1) || '0.0';
      const max = filters.maxGPA?.toFixed(1) || '4.0';
      chips.push({
        label: `GPA: ${min} - ${max}`,
        filterKey: 'gpa',
        value: { min: filters.minGPA, max: filters.maxGPA },
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
    this.setupSearchDebounce();
    this.checkForSavedSearchInUrl(); // NEW: Check if loading a saved search
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

  // NEW: Check if URL has savedSearchId parameter
  private checkForSavedSearchInUrl(): void {
    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        if (params['savedSearchId']) {
          this.loadSavedSearch(+params['savedSearchId']);
        }
      });
  }

  // NEW: Load a saved search by ID
  loadSavedSearch(savedSearchId: number): void {
    this.isLoading.set(true);
    this.savedSearchService.executeSavedSearch(savedSearchId).subscribe({
      next: (response: any) => {
        // Convert backend response to student search format
        const studentFilters = this.filterConverter.toStudentSearchFormat(
          response.appliedFilters
        );
        
        // Apply filters to form
        this.applyFiltersToForm(studentFilters);
        
        // Update current filters
        this.currentFilters.set(studentFilters);
        
        // Display results
        this.searchResults.set(response.results);
        this.totalResults.set(response.totalCount);
        this.currentPage.set(response.page);
        
        this.isLoading.set(false);
      },
      error: (error: any) => {
        console.error('Failed to load saved search:', error);
        this.isLoading.set(false);
      }
    });
  }

  // NEW: Handle saved search selection from dropdown
  onSavedSearchSelected(savedSearchId: number): void {
    this.loadSavedSearch(savedSearchId);
  }

  // NEW: Open save search modal
  openSaveModal(): void {
    this.isSaveModalOpen.set(true);
  }

  // NEW: Close save search modal
  closeSaveModal(): void {
    this.isSaveModalOpen.set(false);
  }

  // NEW: Handle search saved event
  onSearchSaved(): void {
    this.closeSaveModal();
    // Optionally show a success toast/message
    console.log('Search saved successfully!');
  }

  // NEW: Get current filters in backend format for saving
  getCurrentFiltersForSave(): SearchFilterCriteria {
    const currentFilters = this.getFiltersFromForm();
    return this.filterConverter.toSavedSearchFormat(currentFilters);
  }

  private applyFiltersToForm(filters: StudentSearchFilters): void {
    this.filterForm.patchValue({
      instruments: filters.instruments || [],
      states: filters.states || [],
      isHBCU: filters.isHBCU || false,
      distance: filters.distance,
      zipCode: filters.zipCode || '',
      minGPA: filters.minGPA,
      maxGPA: filters.maxGPA,
      graduationYears: filters.graduationYears || [],
      majors: filters.majors || [],
      skillLevels: filters.skillLevels || [],
      minYearsExperience: filters.minYearsExperience,
      maxYearsExperience: filters.maxYearsExperience,
      hasVideo: filters.hasVideo || false,
      hasAuditionVideo: filters.hasAuditionVideo || false,
      isAvailable: filters.isAvailable || false,
      isActivelyRecruiting: filters.isActivelyRecruiting || false,
      hasScholarshipOffers: filters.hasScholarshipOffers || false,
      lastActivityDays: filters.lastActivityDays,
      sortBy: filters.sortBy || 'relevance',
      sortDirection: filters.sortDirection || 'desc'
    });

    if (filters.searchTerm) {
      this.searchForm.patchValue({
        searchTerm: filters.searchTerm
      });
    }
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
      },
      error: () => {
        this.searchSuggestions.set([]);
        this.showSuggestions.set(false);
      }
    });
  }

  private loadFiltersFromUrl(): void {
    // Existing implementation
  }

  performSearch(): void {
    const filters = this.getFiltersFromForm();
    this.currentFilters.set(filters);
    this.isLoading.set(true);

    this.searchService.searchStudents(filters).subscribe({
      next: response => {
        this.searchResults.set(response.results);
        this.totalResults.set(response.totalCount);
        this.currentPage.set(response.page);
        this.pageSize.set(response.pageSize);
        this.isLoading.set(false);
      },
      error: error => {
        console.error('Search error:', error);
        this.searchResults.set([]);
        this.totalResults.set(0);
        this.isLoading.set(false);
      }
    });
  }

  private getFiltersFromForm(): StudentSearchFilters {
    const filterValues = this.filterForm.value;
    const searchTerm = this.searchForm.value.searchTerm;

    return {
      searchTerm: searchTerm || undefined,
      instruments: filterValues.instruments,
      states: filterValues.states,
      isHBCU: filterValues.isHBCU,
      distance: filterValues.distance,
      zipCode: filterValues.zipCode,
      minGPA: filterValues.minGPA,
      maxGPA: filterValues.maxGPA,
      graduationYears: filterValues.graduationYears,
      majors: filterValues.majors,
      skillLevels: filterValues.skillLevels,
      minYearsExperience: filterValues.minYearsExperience,
      maxYearsExperience: filterValues.maxYearsExperience,
      hasVideo: filterValues.hasVideo,
      hasAuditionVideo: filterValues.hasAuditionVideo,
      isAvailable: filterValues.isAvailable,
      isActivelyRecruiting: filterValues.isActivelyRecruiting,
      hasScholarshipOffers: filterValues.hasScholarshipOffers,
      lastActivityDays: filterValues.lastActivityDays,
      sortBy: filterValues.sortBy,
      sortDirection: filterValues.sortDirection,
      page: this.currentPage(),
      pageSize: this.pageSize()
    };
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
    this.searchForm.reset({ searchTerm: '' });
    this.performSearch();
  }

  removeFilterChip(chip: QuickFilterChip): void {
    const currentValues = this.filterForm.get(chip.filterKey)?.value;
    
    if (chip.filterKey === 'gpa') {
      this.filterForm.patchValue({
        minGPA: null,
        maxGPA: null
      });
    } else if (Array.isArray(currentValues)) {
      const updated = currentValues.filter((v: any) => v !== chip.value);
      this.filterForm.get(chip.filterKey)?.setValue(updated);
    } else {
      this.filterForm.get(chip.filterKey)?.setValue(null);
    }
    
    this.performSearch();
  }

  // Filter panel toggle helpers
  toggleFilterPanel(panel: string): void {
    const panels = this.expandedPanels();
    if (panels.has(panel)) {
      panels.delete(panel);
    } else {
      panels.add(panel);
    }
    this.expandedPanels.set(new Set(panels));
  }

  isPanelExpanded(panel: string): boolean {
    return this.expandedPanels().has(panel);
  }

  // Instrument selection helpers
  toggleInstrument(instrument: string, checked: boolean): void {
    const current = this.filterForm.get('instruments')?.value || [];
    if (checked) {
      this.filterForm.get('instruments')?.setValue([...current, instrument]);
    } else {
      this.filterForm.get('instruments')?.setValue(
        current.filter((i: string) => i !== instrument)
      );
    }
  }

  isInstrumentSelected(instrument: string): boolean {
    const instruments = this.filterForm.get('instruments')?.value || [];
    return instruments.includes(instrument);
  }

  // State selection helpers
  toggleState(state: string, checked: boolean): void {
    const current = this.filterForm.get('states')?.value || [];
    if (checked) {
      this.filterForm.get('states')?.setValue([...current, state]);
    } else {
      this.filterForm.get('states')?.setValue(
        current.filter((s: string) => s !== state)
      );
    }
  }

  isStateSelected(state: string): boolean {
    const states = this.filterForm.get('states')?.value || [];
    return states.includes(state);
  }

  // Pagination
  goToPage(page: number): void {
    this.currentPage.set(page);
    this.performSearch();
  }

  nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.goToPage(this.currentPage() + 1);
    }
  }

  previousPage(): void {
    if (this.currentPage() > 1) {
      this.goToPage(this.currentPage() - 1);
    }
  }

  // Watchlist toggle
  toggleWatchlist(studentId: number, event: Event): void {
    event.stopPropagation();
    this.searchService.toggleWatchlist(studentId);
  }

  isWatchlisted(studentId: number): boolean {
    return this.searchService.isWatchlisted(studentId);
  }

  // Navigation
  viewStudentProfile(studentId: number): void {
    this.router.navigate(['/band-staff/students', studentId]);
  }
}