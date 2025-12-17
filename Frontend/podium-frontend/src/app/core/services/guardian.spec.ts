import { TestBed } from '@angular/core/testing';

import { Guardian } from './guardian';

describe('Guardian', () => {
  let service: Guardian;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Guardian);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
