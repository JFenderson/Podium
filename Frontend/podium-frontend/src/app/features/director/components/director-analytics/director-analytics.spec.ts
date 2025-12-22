import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DirectorAnalytics } from './director-analytics.component';

describe('DirectorAnalytics', () => {
  let component: DirectorAnalytics;
  let fixture: ComponentFixture<DirectorAnalytics>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DirectorAnalytics]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DirectorAnalytics);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
