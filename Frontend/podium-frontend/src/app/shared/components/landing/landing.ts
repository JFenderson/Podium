import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatToolbarModule,
    MatChipsModule
  ],
  templateUrl: './landing.html',
  styleUrls: ['./landing.scss']
})
export class LandingComponent {
  features = [
    {
      icon: 'person',
      title: 'For Students',
      description: 'Showcase your talent with audition videos, connect with college band programs, and receive scholarship offers.',
      color: 'primary',
      items: [
        'Upload audition videos',
        'Build your profile',
        'Receive scholarship offers'
      ]
    },
    {
      icon: 'verified',
      title: 'For Recruiters',
      description: 'Discover talented musicians, review audition videos, send scholarship offers, and build your band program.',
      color: 'accent',
      items: [
        'Search and filter students',
        'Rate student performances',
        'Send scholarship offers'
      ]
    },
    {
      icon: 'shield',
      title: 'Guardian Oversight',
      description: 'Parents and guardians can monitor student activities and approve scholarship offers for minors.',
      color: 'warn',
      items: [
        'Monitor student activities',
        'Approve scholarship offers',
        'Safe communication'
      ]
    }
  ];
}