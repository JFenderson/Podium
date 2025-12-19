import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VideoManagement } from './video-management';

describe('VideoManagement', () => {
  let component: VideoManagement;
  let fixture: ComponentFixture<VideoManagement>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VideoManagement]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VideoManagement);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
