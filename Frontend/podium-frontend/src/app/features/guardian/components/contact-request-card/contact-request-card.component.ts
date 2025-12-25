import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { GuardianContactRequestDto } from '../../../../core/models/guardian.models';

@Component({
  selector: 'app-contact-request-card',
  standalone: true,
  imports: [CommonModule, DatePipe],
  template: `
    <div class="bg-white rounded-lg border border-gray-200 shadow-sm p-4 hover:shadow-md transition-shadow">
      <div class="flex justify-between items-start mb-3">
        <div class="flex items-center space-x-3">
          <div class="h-10 w-10 rounded-full bg-indigo-100 flex items-center justify-center text-indigo-600 font-bold text-sm">
            {{ request.recruiterName.charAt(0) }}
          </div>
          <div>
            <h4 class="text-sm font-bold text-gray-900">{{ request.recruiterName }}</h4>
            <p class="text-xs text-gray-500">{{ request.bandName }}</p>
          </div>
        </div>
        <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-amber-100 text-amber-800">
          Approval Required
        </span>
      </div>

      <div class="bg-gray-50 rounded p-3 mb-3">
        <p class="text-xs text-gray-600 italic">"{{ request.message }}"</p>
        <div class="mt-2 text-xs text-gray-400 flex justify-between">
          <span>For: <span class="font-semibold text-gray-700">{{ request.studentName }}</span></span>
          <span>{{ request.sentAt | date:'shortDate' }}</span>
        </div>
      </div>

      <div class="grid grid-cols-2 gap-3">
        <button (click)="onDecline.emit(request.requestId)" 
          class="flex justify-center py-2 px-4 border border-gray-300 rounded-md shadow-sm text-xs font-medium text-gray-700 bg-white hover:bg-gray-50">
          Decline
        </button>
        <button (click)="onApprove.emit(request.requestId)" 
          class="flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-xs font-medium text-white bg-indigo-600 hover:bg-indigo-700">
          Approve
        </button>
      </div>
    </div>
  `
})
export class ContactRequestCardComponent {
  @Input({ required: true }) request!: GuardianContactRequestDto;
  @Output() onApprove = new EventEmitter<number>();
  @Output() onDecline = new EventEmitter<number>();
}