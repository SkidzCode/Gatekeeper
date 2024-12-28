import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Notification } from '../../models/notification.model'; // Adjust the path to your Notification model

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  // Base endpoint for your .NET Core Web API. Adjust if needed (e.g., environment-specific).
  private baseUrl = '/api/Notification';

  constructor(private http: HttpClient) { }

  /**
   * Retrieves all notifications via GET /api/Notification
   */
  getAllNotifications(): Observable<Notification[]> {
    return this.http.get<Notification[]>(this.baseUrl);
  }

  /**
   * Retrieves notifications for a specific user (recipient).
   * GET /api/Notification/user/{recipientId}
   * @param recipientId The Id of the recipient/user.
   */
  getNotificationsByUser(recipientId: number): Observable<Notification[]> {
    const url = `${this.baseUrl}/user/${recipientId}`;
    return this.http.get<Notification[]>(url);
  }

  /**
   * Retrieves notifications that have not been sent
   * and are scheduled on or before the current time.
   * GET /api/Notification/not-sent
   */
  getNotSentNotifications(): Observable<Notification[]> {
    const url = `${this.baseUrl}/not-sent`;
    return this.http.get<Notification[]>(url);
  }

  /**
   * Inserts a new notification into the database.
   * POST /api/Notification
   * @param notification The notification to create on the server.
   * The server returns { message, newNotificationId } upon success.
   */
  addNotification(
    notification: Pick<Notification, 'recipientId' | 'channel' | 'message' | 'scheduledAt'>
  ): Observable<{ message: string; newNotificationId: number }> {
    return this.http.post<{ message: string; newNotificationId: number }>(
      this.baseUrl,
      notification
    );
  }
}
