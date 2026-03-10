import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DirectorAnalyticsComponent } from './director-analytics.component';

describe('DirectorAnalyticsComponent', () => {
  let component: DirectorAnalyticsComponent;
  let fixture: ComponentFixture<DirectorAnalyticsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DirectorAnalyticsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DirectorAnalyticsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
