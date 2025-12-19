import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BandStaff } from './band-staff-list';

describe('BandStaff', () => {
  let component: BandStaff;
  let fixture: ComponentFixture<BandStaff>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BandStaff]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BandStaff);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
