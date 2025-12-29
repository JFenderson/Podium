import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { SavedSearchService } from '../../services/saved-search.service';
import { SavedSearchSummary } from '../../../../core/models/saved-search.models';
import { SaveSearchModalComponent } from '../save-search-modal/save-search-modal.component';

@Component({
  selector: 'app-manage-saved-searches',
  standalone: true,
  imports: [CommonModule, RouterModule, SaveSearchModalComponent],
  templateUrl: './manage-saved-searches.component.html',
  styleUrls: ['./manage-saved-searches.component.scss']
})
export class ManageSavedSearchesComponent implements OnInit {
  savedSearches: SavedSearchSummary[] = [];
  isLoading = false;
  
  // Modal state
  isEditModalOpen = false;
  editingSearchId?: number;
  
  // Share modal
  shareUrl = '';
  showShareModal = false;
  copiedToClipboard = false;

  constructor(
    private savedSearchService: SavedSearchService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadSavedSearches();
  }

  loadSavedSearches(): void {
    this.isLoading = true;
    this.savedSearchService.getSavedSearches().subscribe({
      next: (searches: SavedSearchSummary[]) => {
        this.savedSearches = searches;
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Failed to load saved searches:', error);
        this.isLoading = false;
      }
    });
  }

  runSearch(searchId: number): void {
    // Navigate to search page with the saved search ID
    this.router.navigate(['/recruiter/students/search'], {
      queryParams: { savedSearchId: searchId }
    });
  }

  editSearch(searchId: number): void {
    this.editingSearchId = searchId;
    this.isEditModalOpen = true;
  }

  deleteSearch(searchId: number, searchName: string): void {
    if (!confirm(`Are you sure you want to delete "${searchName}"?`)) {
      return;
    }

    this.savedSearchService.deleteSavedSearch(searchId).subscribe({
      next: () => {
        this.loadSavedSearches();
      },
      error: (error: any) => {
        console.error('Failed to delete search:', error);
        alert('Failed to delete search. Please try again.');
      }
    });
  }

  shareSearch(searchId: number): void {
    this.savedSearchService.shareSearch(searchId).subscribe({
      next: (result: any) => {
        this.shareUrl = window.location.origin + result.shareUrl;
        this.showShareModal = true;
        this.copiedToClipboard = false;
      },
      error: (error: any) => {
        console.error('Failed to share search:', error);
        alert('Failed to generate share link. Please try again.');
      }
    });
  }

  unshareSearch(searchId: number): void {
    this.savedSearchService.unshareSearch(searchId).subscribe({
      next: () => {
        this.loadSavedSearches();
      },
      error: (error: any) => {
        console.error('Failed to unshare search:', error);
        alert('Failed to unshare search. Please try again.');
      }
    });
  }

  copyShareUrl(): void {
    navigator.clipboard.writeText(this.shareUrl).then(() => {
      this.copiedToClipboard = true;
      setTimeout(() => {
        this.copiedToClipboard = false;
      }, 2000);
    });
  }

  closeShareModal(): void {
    this.showShareModal = false;
    this.shareUrl = '';
  }

  onSearchUpdated(): void {
    this.isEditModalOpen = false;
    this.loadSavedSearches();
  }

  formatDate(date?: Date): string {
    if (!date) return 'Never';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  getAlertFrequencyLabel(days?: number): string {
    if (!days) return '';
    if (days === 1) return 'Daily';
    if (days === 7) return 'Weekly';
    if (days === 30) return 'Monthly';
    return `Every ${days} days`;
  }
}