import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

export interface BreadcrumbItem {
  label: string;
  url?: string;
  icon?: string;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <nav class="flex items-center space-x-2 text-sm mb-4" aria-label="Breadcrumb">
      <a routerLink="/" class="text-gray-500 hover:text-gray-700 transition">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"></path>
        </svg>
      </a>

      <ng-container *ngFor="let item of items; let last = last">
        <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
        </svg>

        <a *ngIf="!last && item.url" 
           [routerLink]="item.url"
           class="text-gray-500 hover:text-gray-700 transition font-medium">
          {{ item.label }}
        </a>

        <span *ngIf="last" 
              class="text-gray-900 font-semibold"
              aria-current="page">
          {{ item.label }}
        </span>

        <span *ngIf="!last && !item.url" 
              class="text-gray-500 font-medium">
          {{ item.label }}
        </span>
      </ng-container>
    </nav>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class BreadcrumbComponent {
  @Input() items: BreadcrumbItem[] = [];
}