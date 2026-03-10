import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VideoPlayerRatingComponent } from './video-rating.component';

describe('VideoPlayerRatingComponent', () => {
  let component: VideoPlayerRatingComponent;
  let fixture: ComponentFixture<VideoPlayerRatingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VideoPlayerRatingComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VideoPlayerRatingComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
