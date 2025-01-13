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

  /** Form for creating a new notification */
  notificationForm!: FormGroup;

  /** Form for managing or filtering templates */
  templateSearchForm!: FormGroup;

  /** All available templates */
  templates: NotificationTemplate[] = [];

  /** Templates after filtering/search */
  filteredTemplates: NotificationTemplate[] = [];

  /** All registered users (to pick as recipients) */
  users: User[] = [];

  /** Log of notifications that have been created or fetched from server */
  notificationLog: Notification[] = [];

  /** Loading indicators */
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

  /**
   * Initialize the reactive forms used in this component.
   */
  initializeForms(): void {
    // Form to create or schedule a notification
    this.notificationForm = this.fb.group({
      recipientId: [null, Validators.required],
      channel: ['email', Validators.required], // default to 'email'
      subject: ['', Validators.required],
      message: ['', Validators.required],
      scheduledAt: [null] // optional
    });

    // Form to filter notification templates
    this.templateSearchForm = this.fb.group({
      searchValue: ['']
    });
  }

  /**
   * Fetch all available notification templates.
   */
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

  /**
   * Fetch all users to populate the recipient list.
   */
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

  /**
   * Fetch all notifications for a log or status table.
   */
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

  /**
   * Filter the templates based on the search value.
   */
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

  /**
   * Load the selected template into the notification form.
   * @param template The selected NotificationTemplate
   */
  applyTemplate(template: NotificationTemplate): void {
    this.notificationForm.patchValue({
      channel: template.channel,
      subject: template.subject,
      message: template.body
    });
    this.showSnackBar(`Template "${template.templateName}" applied to form`);
  }

  /**
   * Preview the notification using a Material Dialog before sending.
   */
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

    // Optionally handle the dialog close event
    dialogRef.afterClosed().subscribe((result) => {
      if (result?.send) {
        this.submitNotification();
      }
    });
  }

  /**
   * Submit the notification form to create a new notification in the database.
   */
  submitNotification(): void {
    if (this.notificationForm.invalid) {
      this.showSnackBar('Please fill out all required fields');
      return;
    }

    const formValue = this.notificationForm.value;

    // Instead of Partial<Notification>...
    const newNotification: Pick<Notification, 'recipientId' | 'channel' | 'subject' | 'message' | 'scheduledAt'> = {
      recipientId: formValue.recipientId, // Must be a number
      channel: formValue.channel,
      subject: formValue.subject,// e.g., 'email'
      message: formValue.message,
      scheduledAt: formValue.scheduledAt
        ? formValue.scheduledAt.toISOString()
        : null
    };

    // Now pass newNotification to the service
    this.notificationService.addNotification(newNotification).subscribe({
      next: (res) => {
        this.showSnackBar('Notification created successfully');
        // Refresh the log
        this.fetchNotificationLog();
        // Reset form if desired
        this.notificationForm.reset({ channel: 'email' });
      },
      error: (err) => {
        console.error('Error creating notification:', err);
        this.showSnackBar('Error creating notification');
      }
    });

  }

  /**
   * Utility method to show a Material Snackbar message.
   * @param message The message to display
   * @param action The action label (optional)
   * @param duration How long in ms the snack bar should be shown (default 3s)
   */
  private showSnackBar(message: string, action = 'OK', duration = 3000): void {
    this.snackBar.open(message, action, { duration });
  }
}
