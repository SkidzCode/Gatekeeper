import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';

import { SessionService } from './session.service';
import { SessionModel } from '../../../shared/models/session.model';

describe('SessionService', () => {
  let service: SessionService;
  let httpMock: HttpTestingController;
  const baseUrl = '/api/Session';

  // Mock SessionModel data
  const mockSessions: SessionModel[] = [
    {
      id: 'session-id-1',
      userId: 123,
      verificationId: 'verification-id-1',
      expiryDate: new Date(new Date().getTime() + 24 * 60 * 60 * 1000), // Expires tomorrow
      complete: true,
      revoked: false,
      createdAt: new Date(new Date().getTime() - 2 * 24 * 60 * 60 * 1000), // Created 2 days ago
      updatedAt: new Date(), // Last activity now
      // Optional fields
      verifyType: 'email_verification',
      verificationExpiryDate: new Date(new Date().getTime() + 24 * 60 * 60 * 1000),
      verificationComplete: true,
      verificationRevoked: false,
    },
    {
      id: 'session-id-2',
      userId: 124, // For testing getActiveSessionsUser
      verificationId: 'verification-id-2',
      expiryDate: new Date(new Date().getTime() + 48 * 60 * 60 * 1000), // Expires in 2 days
      complete: true,
      revoked: false,
      createdAt: new Date(new Date().getTime() - 1 * 24 * 60 * 60 * 1000), // Created 1 day ago
      updatedAt: new Date(new Date().getTime() - 1 * 60 * 60 * 1000), // Last activity 1 hour ago
      verifyType: 'sms_verification',
      verificationComplete: true,
    },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SessionService],
    });
    service = TestBed.inject(SessionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Make sure that there are no outstanding requests.
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getActiveSessions', () => {
    it('should return active sessions on success', () => {
      service.getActiveSessions().subscribe((sessions) => {
        expect(sessions.length).toBe(2);
        expect(sessions).toEqual(mockSessions);
      });

      const req = httpMock.expectOne(`${baseUrl}/sessions/active`);
      expect(req.request.method).toBe('GET');
      req.flush(mockSessions);
    });

    it('should handle error when fetching active sessions', () => {
      const errorMessage = 'Failed to fetch active sessions';
      service.getActiveSessions().subscribe({
        next: () => fail('should have failed with an error'),
        error: (error) => {
          expect(error).toBe(errorMessage);
        },
      });

      const req = httpMock.expectOne(`${baseUrl}/sessions/active`);
      expect(req.request.method).toBe('GET');
      req.flush({ error: errorMessage }, { status: 500, statusText: 'Server Error' });
    });
  });

  describe('getActiveSessionsUser', () => {
    const userId = 123;
    it('should return active sessions for a specific user on success', () => {
      const userSessions = mockSessions.filter(s => s.userId === userId);
      service.getActiveSessionsUser(userId).subscribe((sessions) => {
        expect(sessions.length).toBe(1);
        expect(sessions).toEqual(userSessions);
      });

      const req = httpMock.expectOne(`${baseUrl}/sessions/activeUser/${userId}`);
      expect(req.request.method).toBe('GET');
      req.flush(userSessions);
    });

    it('should handle error when fetching active sessions for a user', () => {
      const errorMessage = 'Failed to fetch user sessions';
      service.getActiveSessionsUser(userId).subscribe({
        next: () => fail('should have failed with an error'),
        error: (error) => {
          expect(error).toBe(errorMessage);
        },
      });

      const req = httpMock.expectOne(`${baseUrl}/sessions/activeUser/${userId}`);
      expect(req.request.method).toBe('GET');
      req.flush({ error: errorMessage }, { status: 404, statusText: 'Not Found' });
    });
  });

  describe('getMostRecentActivity', () => {
    it('should return recent activity sessions on success', () => {
      service.getMostRecentActivity().subscribe((sessions) => {
        expect(sessions.length).toBe(2); // Assuming mockSessions is used
        expect(sessions).toEqual(mockSessions);
      });

      const req = httpMock.expectOne(`${baseUrl}/sessions/recent`);
      expect(req.request.method).toBe('GET');
      req.flush(mockSessions);
    });

    it('should handle error when fetching recent activity', () => {
      const errorMessage = 'Error: Network error'; // Example of a client-side or network error
      service.getMostRecentActivity().subscribe({
        next: () => fail('should have failed with an error'),
        error: (error) => {
          expect(error).toBe(errorMessage);
        },
      });

      const req = httpMock.expectOne(`${baseUrl}/sessions/recent`);
      expect(req.request.method).toBe('GET');
      // Simulate a client-side/network error
      req.error(new ErrorEvent('Network error', { message: 'Network error' }));
    });
  });

  describe('revokeSession', () => {
    const sessionIdToRevoke = '1';
    it('should successfully revoke a session', () => {
      const mockResponse = { message: 'Session revoked' };
      service.revokeSession(sessionIdToRevoke).subscribe((response) => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/sessions/revoke/${sessionIdToRevoke}`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({}); // Expects an empty body
      req.flush(mockResponse);
    });

    it('should handle error when revoking a session', () => {
      const serverErrorMessage = 'Session ID not found or invalid.';
      service.revokeSession(sessionIdToRevoke).subscribe({
        next: () => fail('should have failed with an error'),
        error: (error) => {
          expect(error).toBe(serverErrorMessage);
        },
      });

      const req = httpMock.expectOne(`${baseUrl}/sessions/revoke/${sessionIdToRevoke}`);
      expect(req.request.method).toBe('POST');
      // Example of a server-side error with a specific structure the handleError method might parse
      req.flush({ error: serverErrorMessage }, { status: 400, statusText: 'Bad Request' });
    });

    it('should handle plain HttpErrorResponse message (e.g. network or non-standard server error)', () => {
        // This test aims to cover the branch in `handleError` where `error.error` is not an `ErrorEvent`
        // and `error.error.error` (the custom server message path) is not present, so it falls back to `error.message`.
        const genericErrorMessage = 'Http failure response for /api/Session/sessions/revoke/1: 503 Service Unavailable';

        service.revokeSession(sessionIdToRevoke).subscribe({
          next: () => fail('should have failed with an error'),
          error: (error) => {
            // The actual message from HttpErrorResponse when flushed with null body and status
            // will be something like "Http failure response for /api/Session/sessions/revoke/1: 503 Service Unavailable"
            expect(error).toContain('Http failure response');
            expect(error).toContain('503 Service Unavailable');
          },
        });

        const req = httpMock.expectOne(`${baseUrl}/sessions/revoke/${sessionIdToRevoke}`);
        expect(req.request.method).toBe('POST');
        // Flushing with a null body and an error status will generate an HttpErrorResponse
        // whose `message` property is a default http error string.
        req.flush(null, { status: 503, statusText: 'Service Unavailable' });
      });
  });
});
