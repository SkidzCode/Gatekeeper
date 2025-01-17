export interface Notification {
  id?: number;            // For new notifications, the server will generate the Id
  recipientId: number;
  fromId?: number;
  toName?: string;
  toEmail?: string;
  channel: 'email' | 'sms' | 'push' | 'inapp';
  url?: string;
  tokenType: string;
  subject: string;
  message: string;
  isSent?: boolean;       // The server might handle sent status
  scheduledAt?: string | null;
  createdAt?: string;     // Timestamps may be returned from the server
  updatedAt?: string;
}
