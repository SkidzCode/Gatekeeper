import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-notification-preview-dialog',
  template: `
    <h1 mat-dialog-title>Notification Preview</h1>
    <div mat-dialog-content>
      <h3>{{ data.subject }}</h3>
      <p>{{ data.message }}</p>
    </div>
    <div mat-dialog-actions>
      <button mat-button (click)="onClose()">Cancel</button>
      <button mat-raised-button color="primary" (click)="onSend()">Send</button>
    </div>
  `,
  standalone: false,
})
export class NotificationPreviewDialogComponent {
  constructor(
    @Inject(MAT_DIALOG_DATA) public data: { subject: string; message: string },
    private dialogRef: MatDialogRef<NotificationPreviewDialogComponent>
  ) { }

  onClose(): void {
    this.dialogRef.close();
  }

  onSend(): void {
    // Pass back a flag indicating the user wants to send.
    this.dialogRef.close({ send: true });
  }
}
