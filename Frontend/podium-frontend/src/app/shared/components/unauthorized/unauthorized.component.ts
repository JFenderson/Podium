import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="flex justify-center items-center min-h-screen bg-background p-4">
      <div class="bg-surface p-10 rounded-lg shadow-lg max-w-lg w-full text-center border border-border">
        <div class="mb-6 inline-flex p-4 rounded-full bg-red-100">
          <span class="material-icons text-6xl text-red-500">block</span>
        </div>
        <h1 class="text-3xl font-bold text-text-primary mb-2">Access Denied</h1>
        <p class="text-text-secondary mb-8">You don't have permission to access this page.</p>
        <a routerLink="/dashboard" class="inline-block px-6 py-3 bg-primary text-white font-medium rounded-md hover:bg-primary-dark transition-colors">
          Go to Dashboard
        </a>
      </div>
    </div>
  `,
  styles: []
})
export class UnauthorizedComponent {}