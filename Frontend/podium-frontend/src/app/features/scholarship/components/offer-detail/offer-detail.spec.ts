import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OfferDetail } from './offer-detail';

describe('OfferDetail', () => {
  let component: OfferDetail;
  let fixture: ComponentFixture<OfferDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OfferDetail]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OfferDetail);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
