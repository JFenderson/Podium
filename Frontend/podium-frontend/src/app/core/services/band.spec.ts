import { TestBed } from '@angular/core/testing';

import { Band } from './band';

describe('Band', () => {
  let service: Band;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Band);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
