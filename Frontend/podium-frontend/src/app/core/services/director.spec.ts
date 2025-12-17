import { TestBed } from '@angular/core/testing';

import { Director } from './director';

describe('Director', () => {
  let service: Director;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Director);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
