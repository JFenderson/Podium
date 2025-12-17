import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BandList } from './band-list';

describe('BandList', () => {
  let component: BandList;
  let fixture: ComponentFixture<BandList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BandList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BandList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
