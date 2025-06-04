import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';

import { NotificationService } from './notification.service';
// Corrected import based on NotificationService and model file
import { Notification } from '../../../shared/models/notification.model';

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;

  const baseUrl = '/api/Notification';

  const mockNotification: Notification = {
    id: 1,
    recipientId: 123,
    fromId: 100,
    toName: 'Test User',
    toEmail: 'test@example.com',
    channel: 'inapp',
    url: '/notifications/1',
    tokenType: 'bearer', // Added mandatory field
    subject: 'Test Notification', // Changed from title
    message: 'This is a test notification.',
    isSent: false,
    scheduledAt: null,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  };

  const mockNotifications: Notification[] = [
    mockNotification,
    {
      id: 2,
      recipientId: 456,
      channel: 'email',
      subject: 'Another Notification',
      message: 'Another test.',
      tokenType: 'none', // Added mandatory field
      isSent: true,
      createdAt: new Date().toISOString()
      // Other fields can be optional as per model
    }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [NotificationService]
    });
    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Ensure no outstanding requests
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  // Placeholder for more tests

  // Tests for getAllNotifications
  describe('getAllNotifications', () => {
    it('should retrieve all notifications via GET request', () => {
      service.getAllNotifications().subscribe(notifications => {
        expect(notifications.length).toBe(2);
        expect(notifications).toEqual(mockNotifications);
      });

      const req = httpMock.expectOne(baseUrl); // Endpoint is just the baseUrl
      expect(req.request.method).toBe('GET');
      req.flush(mockNotifications);
    });

    it('should handle errors for getAllNotifications', () => {
      const errorMessage = 'Error fetching all notifications';
      service.getAllNotifications().subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: HttpErrorResponse) => { // Assuming generic HttpErrorResponse for now
          expect(error.status).toBe(500);
        }
      });

      const req = httpMock.expectOne(baseUrl);
      req.flush({ message: errorMessage }, { status: 500, statusText: 'Server Error' });
    });
  });

  // Tests for getNotificationsByUser
  describe('getNotificationsByUser', () => {
    const recipientId = 123;
    const userNotifications = [mockNotification]; // Filtered or specific mock

    it('should retrieve notifications for a specific user via GET request', () => {
      service.getNotificationsByUser(recipientId).subscribe(notifications => {
        expect(notifications.length).toBe(1);
        expect(notifications).toEqual(userNotifications);
      });

      const req = httpMock.expectOne(`${baseUrl}/user/${recipientId}`);
      expect(req.request.method).toBe('GET');
      req.flush(userNotifications);
    });

    it('should return an empty array if user has no notifications (API returns empty array)', () => {
      const otherRecipientId = 999;
      service.getNotificationsByUser(otherRecipientId).subscribe(notifications => {
        expect(notifications.length).toBe(0);
        expect(notifications).toEqual([]);
      });

      const req = httpMock.expectOne(`${baseUrl}/user/${otherRecipientId}`);
      req.flush([]); // Simulate API returning an empty array for this user
    });

    it('should handle errors for getNotificationsByUser', () => {
      const recipientIdWithError = 789;
      service.getNotificationsByUser(recipientIdWithError).subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(500);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/user/${recipientIdWithError}`);
      req.flush({ message: 'Error' }, { status: 500, statusText: 'Server Error' });
    });
  });

  // Tests for getNotSentNotifications
  describe('getNotSentNotifications', () => {
    const notSentNotifications = mockNotifications.filter(n => !n.isSent);

    it('should retrieve not-sent notifications via GET request', () => {
      service.getNotSentNotifications().subscribe(notifications => {
        // Depending on mock data, this might be one or more.
        expect(notifications.some(n => !n.isSent)).toBeTrue();
        // Or if more specific: expect(notifications).toEqual(notSentNotifications);
      });

      const req = httpMock.expectOne(`${baseUrl}/not-sent`);
      expect(req.request.method).toBe('GET');
      req.flush(notSentNotifications); // Use the filtered mock data
    });

    it('should return an empty array if no not-sent notifications are found', () => {
      service.getNotSentNotifications().subscribe(notifications => {
        expect(notifications.length).toBe(0);
        expect(notifications).toEqual([]);
      });

      const req = httpMock.expectOne(`${baseUrl}/not-sent`);
      req.flush([]);
    });

    it('should handle errors for getNotSentNotifications', () => {
      service.getNotSentNotifications().subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(500);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/not-sent`);
      req.flush({ message: 'Error' }, { status: 500, statusText: 'Server Error' });
    });
  });

  // Tests for addNotification
  describe('addNotification', () => {
    // Payload for addNotification uses Pick type
    const newNotificationPayload: Pick<Notification, 'recipientId' | 'channel' | 'message' | 'scheduledAt' | 'subject' | 'tokenType'> = {
      recipientId: 789,
      channel: 'push',
      message: 'Your new notification message',
      scheduledAt: null, // Or a specific date string
      subject: 'New Push', // Subject is mandatory in Notification model
      tokenType: 'fcm' // TokenType is mandatory
    };
    const expectedResponse = { message: 'Notification created', newNotificationId: 10 };

    it('should send new notification data via POST request', () => {
      service.addNotification(newNotificationPayload).subscribe(response => {
        expect(response).toEqual(expectedResponse);
      });

      const req = httpMock.expectOne(baseUrl); // POST to baseUrl
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newNotificationPayload);
      req.flush(expectedResponse);
    });

    it('should handle errors for addNotification (e.g., validation error 400)', () => {
      const invalidPayload: any = { ...newNotificationPayload, message: '' }; // Example of invalid data

      service.addNotification(invalidPayload).subscribe({
        next: () => fail('should have failed with a 400 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(400);
        }
      });

      const req = httpMock.expectOne(baseUrl);
      req.flush({ error: 'Validation failed', errors: { message: ['Message is required'] } }, { status: 400, statusText: 'Bad Request' });
    });

    it('should handle server errors for addNotification (e.g., 500)', () => {
      service.addNotification(newNotificationPayload).subscribe({
        next: () => fail('should have failed with a 500 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(500);
        }
      });

      const req = httpMock.expectOne(baseUrl);
      req.flush({ message: 'Internal server error during notification creation' }, { status: 500, statusText: 'Server Error' });
    });
  });
});
