// site/models/notification-template.model.ts

export interface NotificationTemplate {
  templateId?: number;
  templateName: string;
  channel: 'email' | 'sms' | 'push' | 'inapp';
  tokenType: string;
  subject: string;
  body: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}
