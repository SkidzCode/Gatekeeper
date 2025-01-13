import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { NotificationService } from '../../../../../src/app/services/site/notification.service';
import { NotificationTemplateService } from '../../../../../src/app/services/site/notification-template.service';
import { UserService } from '../../../../../src/app/services/user/user.service';
import { Notification } from '../../../../../src/app/models/notification.model';
import { NotificationTemplate } from '../../../../../src/app/models/notification.template.model';
import { User } from '../../../../../src/app/models/user.model';
import { NotificationPreviewDialogComponent } from '../notification-preview-dialog/notification-preview-dialog.component';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-notification',
  templateUrl: './notification.component.html',
  styleUrls: ['./notification.component.scss'],
  standalone: false,
})
export class NotificationComponent implements OnInit {

  notificationForm!: FormGroup;
  templateSearchForm!: FormGroup;
  templates: NotificationTemplate[] = [];
  filteredTemplates: NotificationTemplate[] = [];
  users: User[] = [];
  notificationLog: Notification[] = [];
  loadingTemplates = false;
  loadingUsers = false;
  loadingNotifications = false;

  constructor(
    private fb: FormBuilder,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private notificationService: NotificationService,
    private notificationTemplateService: NotificationTemplateService,
    private userService: UserService
  ) { }

  ngOnInit(): void {
    this.initializeForms();
    this.fetchTemplates();
    this.fetchUsers();
    this.fetchNotificationLog();
  }

  initializeForms(): void {
    this.notificationForm = this.fb.group({
      recipientId: [null, Validators.required],
      channel: ['email', Validators.required],
      subject: ['', Validators.required],
      message: ['', Validators.required],
      scheduledAt: [null]
    });
    this.templateSearchForm = this.fb.group({
      searchValue: ['']
    });
  }

  // 1. Identify the currently selected user
  get selectedUser(): User | null {
    const recipientId = this.notificationForm.value.recipientId;
    return this.users.find(u => u.id === recipientId) || null;
  }

  // 2. Determine which tokens appear in the message and show their values
  get variableReplacements(): { key: string; value: string }[] {
    const msg = this.notificationForm.value.message || '';
    const user = this.selectedUser;
    const replacements: { key: string; value: string }[] = [];

    if (!msg) {
      return replacements;
    }

    if (msg.includes('{{First_Name}}') && user) {
      replacements.push({ key: '{{First_Name}}', value: user.firstName });
    }
    if (msg.includes('{{Last_Name}}') && user) {
      replacements.push({ key: '{{Last_Name}}', value: user.lastName });
    }
    if (msg.includes('{{Email}}') && user) {
      replacements.push({ key: '{{Email}}', value: user.email });
    }
    if (msg.includes('{{Username}}') && user) {
      replacements.push({ key: '{{Username}}', value: user.username || '' });
    }
    if (msg.includes('{{URL}}')) {
      replacements.push({ key: '{{URL}}', value: window.location.origin });
    }
    if (msg.includes('{{Verification_Code}}')) {
      replacements.push({ key: '{{Verification_Code}}', value: 'Generated server-side' });
    }

    return replacements;
  }

  // Existing Methods ...
  fetchTemplates(): void {
    this.loadingTemplates = true;
    this.notificationTemplateService.getAllNotificationTemplates().subscribe({
      next: (templates) => {
        this.templates = templates;
        this.filteredTemplates = [...templates];
        this.loadingTemplates = false;
      },
      error: (err) => {
        console.error('Error fetching templates:', err);
        this.showSnackBar('Error fetching templates');
        this.loadingTemplates = false;
      }
    });
  }

  fetchUsers(): void {
    this.loadingUsers = true;
    this.userService.getUsers().subscribe({
      next: (users) => {
        this.users = users;
        this.loadingUsers = false;
      },
      error: (err) => {
        console.error('Error fetching users:', err);
        this.showSnackBar('Error fetching users');
        this.loadingUsers = false;
      }
    });
  }

  fetchNotificationLog(): void {
    this.loadingNotifications = true;
    this.notificationService.getAllNotifications().subscribe({
      next: (notifications) => {
        this.notificationLog = notifications;
        this.loadingNotifications = false;
      },
      error: (err) => {
        console.error('Error fetching notifications:', err);
        this.showSnackBar('Error fetching notifications');
        this.loadingNotifications = false;
      }
    });
  }

  onTemplateSearchChange(): void {
    const searchValue = this.templateSearchForm.value.searchValue?.toLowerCase() || '';
    if (!searchValue) {
      this.filteredTemplates = [...this.templates];
      return;
    }
    this.filteredTemplates = this.templates.filter((template) =>
      template.templateName.toLowerCase().includes(searchValue) ||
      template.subject.toLowerCase().includes(searchValue) ||
      template.channel.toLowerCase().includes(searchValue)
    );
  }

  applyTemplate(template: NotificationTemplate): void {
    this.notificationForm.patchValue({
      channel: template.channel,
      subject: template.subject,
      message: template.body
    });
    this.showSnackBar(`Template "${template.templateName}" applied to form`);
  }

  previewNotification(): void {
    if (this.notificationForm.invalid) {
      this.showSnackBar('Please fill out required fields before previewing');
      return;
    }
    const dialogRef = this.dialog.open(NotificationPreviewDialogComponent, {
      width: '500px',
      data: {
        subject: this.notificationForm.value.subject,
        message: this.notificationForm.value.message
      }
    });
    dialogRef.afterClosed().subscribe((result) => {
      if (result?.send) {
        this.submitNotification();
      }
    });
  }

  submitNotification(): void {
    if (this.notificationForm.invalid) {
      this.showSnackBar('Please fill out all required fields');
      return;
    }
    const formValue = this.notificationForm.value;
    const newNotification: Pick<Notification, 'recipientId' | 'channel' | 'subject' | 'message' | 'scheduledAt'> = {
      recipientId: formValue.recipientId,
      channel: formValue.channel,
      subject: formValue.subject,
      message: formValue.message,
      scheduledAt: formValue.scheduledAt
        ? formValue.scheduledAt.toISOString()
        : null
    };
    this.notificationService.addNotification(newNotification).subscribe({
      next: () => {
        this.showSnackBar('Notification created successfully');
        this.fetchNotificationLog();
        this.notificationForm.reset({ channel: 'email' });
      },
      error: (err) => {
        console.error('Error creating notification:', err);
        this.showSnackBar('Error creating notification');
      }
    });
  }

  private showSnackBar(message: string, action = 'OK', duration = 3000): void {
    this.snackBar.open(message, action, { duration });
  }

  insertVariable(variable: string) {
    const currentMessage = this.notificationForm.get('message')?.value || '';
    this.notificationForm.get('message')?.setValue(currentMessage + ' ' + variable);
  }

}
