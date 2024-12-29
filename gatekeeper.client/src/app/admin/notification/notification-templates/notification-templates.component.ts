import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NotificationTemplateService } from '../../../services/site/notification-template.service';
import { NotificationTemplate } from '../../../models/notification.template.model';
import { BehaviorSubject } from 'rxjs';
import { ChangeDetectorRef } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser'; // Corrected Import
import { SecurityContext } from '@angular/core'; // Corrected Import

@Component({
  selector: 'app-notification-templates',
  templateUrl: './notification-templates.component.html',
  styleUrls: ['./notification-templates.component.scss'],
  standalone: false
})
export class NotificationTemplatesComponent implements OnInit {
  templates: NotificationTemplate[] = [];
  selectedTemplate: NotificationTemplate | null = null;

  // Use a BehaviorSubject for preview mode
  previewMode$ = new BehaviorSubject<boolean>(false);

  // Channels for the dropdown
  channels: Array<NotificationTemplate['channel']> = ['email', 'sms', 'push', 'inapp'];

  // Reactive form
  templateForm!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private notificationTemplateService: NotificationTemplateService,
    private cdRef: ChangeDetectorRef,
    private sanitizer: DomSanitizer // Inject DomSanitizer
  ) { }

  ngOnInit(): void {
    this.loadTemplates();

    // Initialize form with all required controls
    // Note: subject is conditionally required only if channel is 'email'
    this.templateForm = this.fb.group({
      templateName: [
        '',
        [Validators.required, Validators.maxLength(100)]
      ],
      channel: ['email', Validators.required],
      subject: [''], // We will add conditional validation in setChannelValidation()
      body: ['', Validators.required],
      isActive: [false]
    });

    // Watch for changes to channel to re-validate subject
    this.templateForm.get('channel')?.valueChanges.subscribe((channel) => {
      this.setChannelValidation(channel);
    });
  }

  /**
   * Loads all templates from the service
   */
  loadTemplates(): void {
    this.notificationTemplateService.getAllNotificationTemplates().subscribe({
      next: (templates) => {
        this.templates = templates;
      },
      error: (err) => {
        console.error('Error loading notification templates:', err);
      }
    });
  }

  /**
   * When a user selects a template from the list,
   * we fetch its details (or use what's already loaded) and patch the form.
   */
  selectTemplate(template: NotificationTemplate): void {
    this.selectedTemplate = template;
    this.patchForm(template);
    this.previewMode$.next(false); // reset preview mode

    setTimeout(() => this.cdRef.detectChanges(), 0);
  }

  /**
   * Patches the form with the selected template’s values.
   */
  patchForm(template: NotificationTemplate): void {
    this.templateForm.patchValue({
      templateName: template.templateName,
      channel: template.channel,
      subject: template.subject,
      body: template.body,
      isActive: template.isActive
    });
    this.setChannelValidation(template.channel);
  }

  /**
   * Toggles the HTML preview mode on/off.
   */
  togglePreview(): void {
    const current = this.previewMode$.getValue();
    this.previewMode$.next(!current);
  }

  /**
   * Adjust the validators for the subject field depending on the channel.
   */
  setChannelValidation(channel: NotificationTemplate['channel']): void {
    const subjectControl = this.templateForm.get('subject');
    if (!subjectControl) return;

    // Clear existing validators
    subjectControl.clearValidators();

    // If channel is 'email', subject is required
    if (channel === 'email') {
      subjectControl.setValidators([Validators.required]);
    }

    // Re-validate
    subjectControl.updateValueAndValidity();
  }

  /**
   * Saves the changes. In a real app, you'd call updateNotificationTemplate() on the service.
   * For now, we'll just simulate by calling the method and logging.
   */
  save(): void {
    if (!this.selectedTemplate) {
      return;
    }

    if (this.templateForm.invalid) {
      // Mark all fields as touched to show errors
      this.templateForm.markAllAsTouched();
      return;
    }

    // Construct updated template object
    const updatedTemplate: NotificationTemplate = {
      ...this.selectedTemplate,
      ...this.templateForm.value
    };

    // Simulate saving (real usage: call `updateNotificationTemplate()`)
    this.notificationTemplateService.updateNotificationTemplate(updatedTemplate).subscribe({
      next: (res) => {
        console.log('Template updated successfully:', res);
        // Refresh list or show a success message
      },
      error: (err) => {
        console.error('Error updating template:', err);
      }
    });
  }

  /**
   * Utility helper to check if a form control has an error and is touched/dirty.
   */
  hasError(controlName: string, errorKey: string): boolean {
    const control = this.templateForm.get(controlName);
    return !!control && control.hasError(errorKey) && (control.dirty || control.touched);
  }

  /**
   * Constructs the sanitized HTML content for preview.
   */
  getPreviewHtml(): string {
    const subject = this.escapeHtml(this.templateForm.value.subject || '');
    const body = this.sanitizeHtml(this.templateForm.value.body || '');
    return `${body}`;
  }

  /**
   * Sanitizes the HTML content to prevent XSS attacks.
   * @param content The HTML content to sanitize.
   * @returns A sanitized HTML string.
   */
  private sanitizeHtml(content: string): string {
    // Sanitize the body content
    const sanitized = this.sanitizer.sanitize(SecurityContext.HTML, content);
    return content || sanitized || '';
  }

  /**
   * Escapes HTML characters in the subject to prevent injection.
   * @param text The text to escape.
   * @returns The escaped text.
   */
  private escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }
}
