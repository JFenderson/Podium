import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DirectorDashboard } from './director-dashboard';

describe('DirectorDashboard', () => {
  let component: DirectorDashboard;
  let fixture: ComponentFixture<DirectorDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DirectorDashboard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DirectorDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
