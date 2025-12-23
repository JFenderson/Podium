import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { BandService } from '../../services/band.service';
import { BandSummaryDto, BandFilterDto } from '../../../../core/models/band.models';

@Component({
  selector: 'app-band-list',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './band-list.component.html'
})
export class BandListComponent implements OnInit {
  private bandService = inject(BandService);
  private fb = inject(FormBuilder);

  bands: BandSummaryDto[] = [];
  filteredBands: BandSummaryDto[] = [];
  isLoading = false;
  error: string | null = null;
  states: string[] = [];

  filterForm: FormGroup = this.fb.group({
    search: [''],
    state: ['']
  });

  ngOnInit(): void {
    this.loadBands();
    this.setupFilterListeners();
  }

  loadBands(): void {
    this.isLoading = true;
    this.error = null;

    this.bandService.getBands().subscribe({
      next: (bands: any) => {
        this.bands = bands;
        this.filteredBands = bands;
        this.extractStates(bands);
        this.isLoading = false;
      },
      error: (error: any) => {
        this.error = 'Failed to load bands. Please try again.';
        this.isLoading = false;
        console.error('Error loading bands:', error);
      }
    });
  }

  setupFilterListeners(): void {
    this.filterForm.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.applyFilters();
      });
  }

  applyFilters(): void {
    const { search, state } = this.filterForm.value;
    
    this.filteredBands = this.bands.filter(band => {
      const matchesSearch = !search || 
        band.bandName.toLowerCase().includes(search.toLowerCase()) ||
        band.universityName.toLowerCase().includes(search.toLowerCase()) ||
        band.city.toLowerCase().includes(search.toLowerCase());
      
      const matchesState = !state || band.state === state;
      
      return matchesSearch && matchesState;
    });
  }

  

  extractStates(bands: BandSummaryDto[]): void {
    const stateSet = new Set(bands.map(b => b.state));
    this.states = Array.from(stateSet).sort();
  }

  clearFilters(): void {
    this.filterForm.reset();
    this.filteredBands = this.bands;
  }
}