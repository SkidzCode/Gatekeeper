import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';

import { NotificationSendComponent } from './notification-send.component';

describe('NotificationSendComponent', () => {
  let component: NotificationSendComponent;
  let fixture: ComponentFixture<NotificationSendComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [NotificationSendComponent],
      imports: [HttpClientTestingModule] // Added
    })
    .compileComponents();

    fixture = TestBed.createComponent(NotificationSendComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
