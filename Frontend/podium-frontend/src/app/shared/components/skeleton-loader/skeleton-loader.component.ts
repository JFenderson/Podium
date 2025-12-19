import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-skeleton-loader',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="animate-pulse space-y-4">
      <div class="h-8 bg-gray-200 rounded w-1/3"></div>
      
      <div class="space-y-3">
        <div class="h-4 bg-gray-200 rounded"></div>
        <div class="h-4 bg-gray-200 rounded w-5/6"></div>
        <div class="h-4 bg-gray-200 rounded w-4/6"></div>
      </div>
      
      @if (type === 'grid') {
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
          <div class="h-32 bg-gray-200 rounded"></div>
          <div class="h-32 bg-gray-200 rounded"></div>
          <div class="h-32 bg-gray-200 rounded"></div>
        </div>
      }
    </div>
  `
})
export class SkeletonLoaderComponent {
  @Input() type: 'text' | 'grid' | 'card' = 'text';
}