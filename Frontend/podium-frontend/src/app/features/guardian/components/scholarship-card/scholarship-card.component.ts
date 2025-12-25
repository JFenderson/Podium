import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { GuardianScholarshipDto } from '../../../../core/models/guardian.models';

@Component({
  selector: 'app-scholarship-card',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe],
  template: `
    <div class="bg-white rounded-lg border-l-4 border-l-green-500 shadow-sm p-4 relative overflow-hidden group hover:shadow-md transition-all">
      
      <div class="absolute top-3 right-4 text-right">
        <div class="text-xl font-bold text-green-700">
          {{ offer.amount | currency:'USD':'symbol':'1.0-0' }}
        </div>
        <div class="text-[10px] text-gray-500 uppercase tracking-wider">Per Year</div>
      </div>

      <div class="mb-4 pr-20">
        <h3 class="text-md font-bold text-gray-900">{{ offer.bandName }}</h3>
        <p class="text-xs text-indigo-600 font-semibold uppercase mt-0.5">{{ offer.offerType }}</p>
        <p class="text-xs text-gray-500 mt-2">Student: {{ offer.studentName }}</p>
      </div>

      <div class="flex items-center text-xs text-orange-600 bg-orange-50 px-2 py-1 rounded w-fit mb-4">
        <svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
        Expires {{ offer.expirationDate | date:'mediumDate' }}
      </div>

      <div class="flex space-x-2">
        <button (click)="onDecline.emit(offer.offerId)" class="flex-1 px-3 py-1.5 text-xs font-medium text-gray-600 bg-gray-100 hover:bg-gray-200 rounded">
          Decline
        </button>
        <button (click)="onAccept.emit(offer.offerId)" class="flex-1 px-3 py-1.5 text-xs font-medium text-white bg-green-600 hover:bg-green-700 rounded shadow-sm">
          Accept Offer
        </button>
        <button class="px-2 text-gray-400 hover:text-indigo-600" title="View Details">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
        </button>
      </div>
    </div>
  `
})
export class ScholarshipCardComponent {
  @Input({ required: true }) offer!: GuardianScholarshipDto;
  @Output() onAccept = new EventEmitter<number>();
  @Output() onDecline = new EventEmitter<number>();
}