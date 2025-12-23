import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ScholarshipService } from '../../services/scholarship.service';
import { ScholarshipOfferDto, OfferStatus } from '../../../../core/models/scholarship.models'; // FIX: Imported OfferStatus

@Component({
  selector: 'app-my-offers',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './my-offers.component.html'
})
export class MyOffersComponent implements OnInit {
  offers: ScholarshipOfferDto[] = [];
  isLoading = true;

  constructor(private scholarshipService: ScholarshipService) {}

  ngOnInit(): void {
    this.loadOffers();
  }

  loadOffers(): void {
    this.scholarshipService.getMyOffers().subscribe({
      next: (data) => {
        this.offers = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading offers', err);
        this.isLoading = false;
      }
    });
  }

  // FIX: Updated to use OfferStatus enum values
  getStatusColor(status: OfferStatus): string {
    switch (status) {
      case OfferStatus.Accepted: return 'border-green-500';
      case OfferStatus.Declined: return 'border-red-500';
      case OfferStatus.Withdrawn: return 'border-gray-500';
      // Note: 'PendingGuardianSignature' isn't in your enum, mapped Sent/Draft instead
      case OfferStatus.Sent: return 'border-blue-500'; 
      default: return 'border-gray-300';
    }
  }

  getStatusBadgeClass(status: OfferStatus): string {
    switch (status) {
      case OfferStatus.Accepted: return 'bg-green-100 text-green-800';
      case OfferStatus.Declined: return 'bg-red-100 text-red-800';
      case OfferStatus.Withdrawn: return 'bg-gray-100 text-gray-800';
      case OfferStatus.Sent: return 'bg-blue-100 text-blue-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  }
}