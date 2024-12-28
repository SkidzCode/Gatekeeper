import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NotificationPreviewDialogComponent } from './notification-preview-dialog.component';

describe('NotificationPreviewDialogComponent', () => {
  let component: NotificationPreviewDialogComponent;
  let fixture: ComponentFixture<NotificationPreviewDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [NotificationPreviewDialogComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NotificationPreviewDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
