import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { SavedSearchService } from '../../services/saved-search.service';
import { SearchFilterCriteria } from '../../../../core/models/saved-search.models';

@Component({
  selector: 'app-save-search-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './save-search-modal.component.html',
  styleUrls: ['./save-search-modal.component.scss']
})
export class SaveSearchModalComponent implements OnInit {
  @Input() isOpen = false;
  @Input() filters!: SearchFilterCriteria;
  @Input() editMode = false;
  @Input() savedSearchId?: number;
  
  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  saveForm!: FormGroup;
  isSubmitting = false;
  errorMessage = '';

  alertFrequencyOptions = [
    { value: 1, label: 'Daily' },
    { value: 7, label: 'Weekly' },
    { value: 30, label: 'Monthly' }
  ];

  constructor(
    private fb: FormBuilder,
    private savedSearchService: SavedSearchService
  ) {}

  ngOnInit(): void {
    this.saveForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', Validators.maxLength(500)],
      alertsEnabled: [false],
      alertFrequencyDays: [7]
    });

    // If edit mode, load existing search
    if (this.editMode && this.savedSearchId) {
      this.loadExistingSearch();
    }
  }

  private loadExistingSearch(): void {
    this.savedSearchService.getSavedSearch(this.savedSearchId!).subscribe({
      next: (search: any) => {
        this.saveForm.patchValue({
          name: search.name,
          description: search.description,
          alertsEnabled: search.alertsEnabled,
          alertFrequencyDays: search.alertFrequencyDays || 7
        });
      },
      error: (error: any) => {
        this.errorMessage = 'Failed to load search details';
        console.error(error);
      }
    });
  }

  onSubmit(): void {
    if (this.saveForm.invalid) {
      this.saveForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const formValue = this.saveForm.value;

    if (this.editMode && this.savedSearchId) {
      // Update existing search
      this.savedSearchService.updateSavedSearch(this.savedSearchId, {
        name: formValue.name,
        description: formValue.description,
        alertsEnabled: formValue.alertsEnabled,
        alertFrequencyDays: formValue.alertsEnabled ? formValue.alertFrequencyDays : null
      }).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.saved.emit();
          this.closeModal();
        },
        error: (error: any) => {
          this.isSubmitting = false;
          this.errorMessage = error.error?.message || 'Failed to update search';
        }
      });
    } else {
      // Create new search
      this.savedSearchService.createSavedSearch({
        name: formValue.name,
        description: formValue.description,
        filterCriteria: this.filters,
        alertsEnabled: formValue.alertsEnabled,
        alertFrequencyDays: formValue.alertsEnabled ? formValue.alertFrequencyDays : null,
        isTemplate: false
      }).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.saved.emit();
          this.closeModal();
        },
        error: (error: any) => {
          this.isSubmitting = false;
          this.errorMessage = error.error?.message || 'Failed to save search';
        }
      });
    }
  }

  closeModal(): void {
    this.saveForm.reset();
    this.errorMessage = '';
    this.close.emit();
  }

  get name() {
    return this.saveForm.get('name');
  }

  get description() {
    return this.saveForm.get('description');
  }

  get alertsEnabled() {
    return this.saveForm.get('alertsEnabled');
  }
}