// site/services/notification-template.service.ts

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NotificationTemplate } from '../../models/notification.template.model';

@Injectable({
  providedIn: 'root',
})
export class NotificationTemplateService {
  /**
   * Points to the .NET Controller's base route: /api/NotificationTemplate.
   * Adjust if you have a different URL or environment variable.
   */
  private baseUrl = '/api/NotificationTemplate';

  constructor(private http: HttpClient) { }

  /**
   * Retrieves all notification templates via GET /api/NotificationTemplate.
   */
  getAllNotificationTemplates(): Observable<NotificationTemplate[]> {
    return this.http.get<NotificationTemplate[]>(this.baseUrl);
  }

  /**
   * Retrieves a single notification template by ID.
   * GET /api/NotificationTemplate/{id}
   * @param id The unique identifier of the notification template.
   */
  getNotificationTemplateById(id: number): Observable<NotificationTemplate> {
    const url = `${this.baseUrl}/${id}`;
    return this.http.get<NotificationTemplate>(url);
  }

  /**
   * Creates a new notification template.
   * POST /api/NotificationTemplate
   * @param template A partial NotificationTemplate object to create on the server.
   */
  createNotificationTemplate(
    template: Omit<NotificationTemplate, 'templateId' | 'createdAt' | 'updatedAt'>
  ): Observable<{ message: string; templateId: number }> {
    return this.http.post<{ message: string; templateId: number }>(this.baseUrl, template);
  }

  /**
   * Updates an existing notification template.
   * PUT /api/NotificationTemplate/{id}
   * @param template The updated notification template object, including an existing templateId.
   */
  updateNotificationTemplate(
    template: NotificationTemplate
  ): Observable<{ message: string }> {
    const url = `${this.baseUrl}/${template.templateId}`;
    return this.http.put<{ message: string }>(url, template);
  }

  /**
   * Deletes an existing notification template by ID.
   * DELETE /api/NotificationTemplate/{id}
   * @param id The unique identifier of the notification template to delete.
   */
  deleteNotificationTemplate(id: number): Observable<{ message: string }> {
    const url = `${this.baseUrl}/${id}`;
    return this.http.delete<{ message: string }>(url);
  }
}
