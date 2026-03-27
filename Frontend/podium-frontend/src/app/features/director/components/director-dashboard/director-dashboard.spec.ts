import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DirectorDashboardComponent } from './director-dashboard.component';

describe('DirectorDashboardComponent', () => {
  let component: DirectorDashboardComponent;
  let fixture: ComponentFixture<DirectorDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DirectorDashboardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DirectorDashboardComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
