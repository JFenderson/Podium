import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ScholarshipService } from '../../services/scholarship.service';
import { AuthService } from '../../../auth/services/auth.service';
// FIX: Corrected import path
import { ScholarshipOfferDto, ScholarshipOfferStatus } from '../../../../core/models/scholarship.models'; 
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-offer-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './offer-detail.component.html'
})
export class OfferDetailComponent implements OnInit {
  offer: ScholarshipOfferDto | null = null;
  currentUser: any;
  OfferStatus = ScholarshipOfferStatus; // Expose enum to template
  
  constructor(
    private route: ActivatedRoute,
    private scholarshipService: ScholarshipService,
    private authService: AuthService,
    private toast: ToastService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.currentUserValue;
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadOffer(+id);
    }
  }

  loadOffer(id: number): void {
    this.scholarshipService.getOffer(id).subscribe(data => this.offer = data);
  }

  get isStudent(): boolean { return this.currentUser?.roles.includes('Student'); }
  get isGuardian(): boolean { return this.currentUser?.roles.includes('Guardian'); }
  get isStaff(): boolean { return this.currentUser?.roles.includes('Director') || this.currentUser?.roles.includes('BandStaff'); }

  get canTakeAction(): boolean {
    if (!this.offer) return false;
    
    // Logic based on your OfferStatus enum
    if (this.isStudent && this.offer.status === ScholarshipOfferStatus.Sent) return true;
    
    // Note: If you add 'PendingGuardianSignature' to OfferStatus later, add logic here.
    // For now assuming Guardian acts on 'Sent' or specific logic you requested:
    if (this.isGuardian && this.offer.requiresGuardianApproval && this.offer.status === ScholarshipOfferStatus.Sent) return true;
    
    if (this.isStaff && (this.offer.status === ScholarshipOfferStatus.Sent || this.offer.status === ScholarshipOfferStatus.Draft)) return true;
    
    return false;
  }

  respond(accept: boolean): void {
    if (!this.offer) return;
    
    const action = accept ? 'ACCEPT' : 'DECLINE';
    const msg = this.isGuardian 
      ? `As the Guardian, are you sure you want to ${action} this offer? This is final.`
      : `Are you sure you want to ${action} this offer?`;

    if (!confirm(msg)) return;

    // FIX: Match RespondToOfferDto interface { isAccepted: boolean; notes?: string }
    const dto = { isAccepted: accept, notes: '' };
    
    this.scholarshipService.respondToOffer(this.offer.offerId, dto).subscribe({
      next: () => {
        this.toast.success(`Offer ${action.toLowerCase()}ed successfully.`); // FIX: .success()
        this.loadOffer(this.offer!.offerId); 
      },
      error: (err) => this.toast.error('Action failed.') // FIX: .error()
    });
  }

  rescind(): void {
    if (!this.offer) return;

    const reason = prompt("Please confirm you want to RESCIND this offer. Enter a reason:");
    if (reason === null) return; 
    if (!reason.trim()) {
      alert("A reason is required to rescind.");
      return;
    }

    this.scholarshipService.withdrawOffer(this.offer.offerId, reason).subscribe({
      next: () => {
        this.toast.success('Offer rescinded.'); // FIX: .success()
        this.loadOffer(this.offer!.offerId);
      },
      error: () => this.toast.error('Failed to rescind offer.') // FIX: .error()
    });
  }
}