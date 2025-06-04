import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotificationPreviewDialogComponent } from './notification-preview-dialog.component';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

describe('NotificationPreviewDialogComponent', () => {
  let component: NotificationPreviewDialogComponent;
  let fixture: ComponentFixture<NotificationPreviewDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [NotificationPreviewDialogComponent],
      imports: [
        NoopAnimationsModule,
        MatDialogModule,
        MatButtonModule
      ],
      providers: [
        { provide: MatDialogRef, useValue: { close: jasmine.createSpy('close') } },
        { provide: MAT_DIALOG_DATA, useValue: { subject: 'Test Subject', message: 'Test Message' } }
      ]
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
