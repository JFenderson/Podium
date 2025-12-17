import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Scholarship } from './scholarship';

describe('Scholarship', () => {
  let component: Scholarship;
  let fixture: ComponentFixture<Scholarship>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Scholarship]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Scholarship);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
