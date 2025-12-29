import { Component, OnInit, Output, EventEmitter, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SavedSearchService } from '../../services/saved-search.service';
import { SavedSearchSummary } from '../../../../core/models/saved-search.models';

@Component({
  selector: 'app-saved-searches-dropdown',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './saved-searches-dropdown.component.html',
  styleUrls: ['./saved-searches-dropdown.component.scss']
})
export class SavedSearchesDropdownComponent implements OnInit {
  @Output() searchSelected = new EventEmitter<number>();

  isOpen = false;
  savedSearches: SavedSearchSummary[] = [];
  isLoading = false;

  constructor(
    private savedSearchService: SavedSearchService,
    private elementRef: ElementRef
  ) {}

  ngOnInit(): void {
    this.loadSavedSearches();
  }

  loadSavedSearches(): void {
    this.isLoading = true;
    this.savedSearchService.getSavedSearches().subscribe({
      next: (searches: SavedSearchSummary[]) => {
        this.savedSearches = searches.slice(0, 10); // Show only 10 most recent
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Failed to load saved searches:', error);
        this.isLoading = false;
      }
    });
  }

  toggleDropdown(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      this.loadSavedSearches();
    }
  }

  selectSearch(searchId: number): void {
    this.searchSelected.emit(searchId);
    this.isOpen = false;
  }

  // Close dropdown when clicking outside
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }

  getAlertBadge(search: SavedSearchSummary): string {
    if (!search.alertsEnabled) return '';
    return '🔔';
  }

  formatLastUsed(lastUsed?: Date): string {
    if (!lastUsed) return 'Never used';
    
    const now = new Date();
    const diff = now.getTime() - new Date(lastUsed).getTime();
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    
    if (days === 0) return 'Today';
    if (days === 1) return 'Yesterday';
    if (days < 7) return `${days} days ago`;
    if (days < 30) return `${Math.floor(days / 7)} weeks ago`;
    return `${Math.floor(days / 30)} months ago`;
  }
}