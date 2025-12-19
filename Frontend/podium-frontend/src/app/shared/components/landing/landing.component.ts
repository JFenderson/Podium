import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss']
})
export class LandingComponent {
  features = [
    {
      icon: 'person',
      title: 'For Students',
      description: 'Showcase your talent with audition videos, connect with college band programs, and receive scholarship offers.',
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
      items: [
        'Monitor student activities',
        'Approve scholarship offers',
        'Safe communication'
      ]
    }
  ];
}