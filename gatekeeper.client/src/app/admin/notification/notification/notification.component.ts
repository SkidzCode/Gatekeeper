import { Component, OnInit, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { NotificationService } from '../../../core/services/site/notification.service';
import { NotificationTemplateService } from '../../../core/services/site/notification-template.service';
import { UserService } from '../../../core/services/user/user.service';
import { Notification } from '../../../../../src/app/shared/models/notification.model';
import { NotificationTemplate } from '../../../../../src/app/shared/models/notification.template.model';
import { User } from '../../../../../src/app/shared/models/user.model';
import { NotificationPreviewDialogComponent } from '../notification-preview-dialog/notification-preview-dialog.component';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/user/auth.service'; // Import AuthService
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

@Component({
  selector: 'app-notification',
  templateUrl: './notification.component.html',
  styleUrls: ['./notification.component.scss'],
  standalone: false,
})
export class NotificationComponent implements OnInit, AfterViewInit {
  dataSource = new MatTableDataSource<Notification>([]);
  @ViewChild('paginatorTop') paginatorTop!: MatPaginator;
  @ViewChild('paginatorBottom') paginatorBottom!: MatPaginator;

  @ViewChild('messageInput') messageInput!: ElementRef;
  notificationForm!: FormGroup;
  templateSearchForm!: FormGroup;
  templates: NotificationTemplate[] = [];
  filteredTemplates: NotificationTemplate[] = [];
  users: User[] = [];
  notificationLog: Notification[] = [];
  loadingTemplates = false;
  loadingUsers = false;
  loadingNotifications = false;
  currentUser: User | null = null; // Add currentUser property
  selectedTabIndex = 0; // Add selectedTabIndex property

  constructor(
    private fb: FormBuilder,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private notificationService: NotificationService,
    private notificationTemplateService: NotificationTemplateService,
    private userService: UserService,
    private authService: AuthService // Inject AuthService
  ) { }

  ngOnInit(): void {
    this.initializeForms();
    this.fetchTemplates();
    this.fetchUsers();
    this.loadCurrentUser(); // Load the current user
    this.fetchNotificationLog();
  }

  ngAfterViewInit() {
    // Use the TOP paginator as the "primary" for the dataSource
    this.dataSource.paginator = this.paginatorTop;

    // 1) When the top paginator changes, mirror that on the bottom
    this.paginatorTop.page.subscribe((event: PageEvent) => {
      this.paginatorBottom.pageIndex = event.pageIndex;
      this.paginatorBottom.pageSize = event.pageSize;
    });

    // 2) When the bottom paginator changes, mirror that on the top
    this.paginatorBottom.page.subscribe((event: PageEvent) => {
      this.paginatorTop.pageIndex = event.pageIndex;
      this.paginatorTop.pageSize = event.pageSize;
      this.paginatorTop._changePageSize(event.pageSize);
    });
  }

  initializeForms(): void {
    this.notificationForm = this.fb.group({
      recipientId: [null, Validators.required],
      channel: ['email', Validators.required],
      subject: ['', Validators.required],
      message: ['', Validators.required],
      scheduledAt: [null],
      tokenType: [''] // Add tokenType field
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

    if (msg.includes('{{From_First_Name}}') && user) {
      replacements.push({ key: '{{From_First_Name}}', value: this.currentUser?.firstName || "Your first name" });
    }
    if (msg.includes('{{From_Last_Name}}') && user) {
      replacements.push({ key: '{{From_Last_Name}}', value: this.currentUser?.lastName || "Your last name" });
    }
    if (msg.includes('{{From_Email}}') && user) {
      replacements.push({ key: '{{From_Email}}', value: this.currentUser?.email || "Your email" });
    }
    if (msg.includes('{{From_Username}}') && user) {
      replacements.push({ key: '{{From_Username}}', value: this.currentUser?.username || '' });
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
        this.dataSource.data = notifications; // Update the data source
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
      message: template.body,
      tokenType: template.tokenType // Set tokenType when applying template
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
    const newNotification: Pick<Notification, 'recipientId' | 'channel' | 'subject' | 'message' | 'scheduledAt' | 'tokenType' | 'url'> = {
      recipientId: formValue.recipientId,
      channel: formValue.channel,
      subject: formValue.subject,
      message: formValue.message,
      scheduledAt: formValue.scheduledAt
        ? formValue.scheduledAt.toISOString()
        : null,
      tokenType: formValue.tokenType || '', // Include tokenType in the request
      url: window.location.origin // Include the current URL
    };
    this.notificationService.addNotification(newNotification).subscribe({
      next: () => {
        this.showSnackBar('Notification created successfully');
        this.fetchNotificationLog();
        this.notificationForm.reset({ channel: 'email', tokenType: '' }); // Reset tokenType
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

  // New Method to load the current user
  private loadCurrentUser(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  // New Method to handle tab change
  onTabChange(event: any): void {
    this.selectedTabIndex = event.index;
    if (this.selectedTabIndex === 1 && this.notificationLog.length === 0) {
      this.fetchNotificationLog();
    }
  }
}
