import { TestBed } from '@angular/core/testing';

import { BandStaff } from './band-staff';

describe('BandStaff', () => {
  let service: BandStaff;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BandStaff);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
