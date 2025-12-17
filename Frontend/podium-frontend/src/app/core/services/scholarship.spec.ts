import { TestBed } from '@angular/core/testing';

import { Scholarship } from './scholarship';

describe('Scholarship', () => {
  let service: Scholarship;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Scholarship);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
