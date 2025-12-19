import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VideoRating } from './video-rating';

describe('VideoRating', () => {
  let component: VideoRating;
  let fixture: ComponentFixture<VideoRating>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VideoRating]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VideoRating);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
