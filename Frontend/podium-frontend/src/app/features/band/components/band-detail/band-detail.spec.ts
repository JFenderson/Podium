import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BandDetailComponent } from './band-detail.component';

describe('BandDetailComponent', () => {
  let component: BandDetailComponent;
  let fixture: ComponentFixture<BandDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BandDetailComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BandDetailComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
