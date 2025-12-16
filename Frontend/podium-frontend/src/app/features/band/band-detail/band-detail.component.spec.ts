import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BandDetail } from './band-detail.component';

describe('BandDetail', () => {
  let component: BandDetail;
  let fixture: ComponentFixture<BandDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BandDetail]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BandDetail);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
