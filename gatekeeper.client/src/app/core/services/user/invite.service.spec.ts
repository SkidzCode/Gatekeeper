import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';

import { InviteService, Invite } from './invite.service';
import { WindowRef } from '../utils/window-ref.service'; // Corrected path

describe('InviteService', () => {
  let service: InviteService;
  let httpMock: HttpTestingController;
  const baseUrl = '/api/Invite';

  // Mock for WindowRef
  let mockWindowRef: { nativeWindow: { location: { origin: string } } };

  const mockInvite: Invite = {
    fromId: 1,
    toName: 'John Doe',
    toEmail: 'john.doe@example.com',
    website: '', // Will be set by the service
  };

  const mockInvitesList: Invite[] = [
    { ...mockInvite, id: 1, verificationId: 'v1', created: new Date().toISOString(), isSent: true, website: 'http://test.com' },
    {
      fromId: 1,
      toName: 'Jane Smith',
      toEmail: 'jane.smith@example.com',
      id: 2,
      verificationId: 'v2',
      created: new Date().toISOString(),
      isSent: true,
      website: 'http://test.com'
    }
  ];

  beforeEach(() => {
    mockWindowRef = {
      nativeWindow: {
        location: {
          origin: 'http://mockorigin.com'
          // No need to mock other location properties if not used by the service.
        }
      }
    };

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        InviteService,
        { provide: WindowRef, useValue: mockWindowRef }
      ],
    });
    service = TestBed.inject(InviteService);
    httpMock = TestBed.inject(HttpTestingController);

    // Spy on console logging for checkInviteRequired tests
    spyOn(console, 'log');
    spyOn(console, 'error');
  });

  afterEach(() => {
    httpMock.verify(); // Make sure that there are no outstanding requests.
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('sendInvite', () => {
    it('should send an invite and set the website origin', () => {
      const expectedResponse = { message: 'Invite sent', inviteId: 123 };
      const inviteToSend: Invite = { ...mockInvite };

      service.sendInvite(inviteToSend).subscribe(response => {
        expect(response).toEqual(expectedResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/SendInvite`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.website).toBe('http://mockorigin.com');
      expect(req.request.body.toEmail).toBe(inviteToSend.toEmail);
      req.flush(expectedResponse);
    });

    it('should handle error when sending an invite', () => {
      const errorMessage = 'Failed to send invite';
      const inviteToSend: Invite = { ...mockInvite };

      service.sendInvite(inviteToSend).subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: Error) => {
          expect(error.message).toBe(errorMessage);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/SendInvite`);
      expect(req.request.method).toBe('POST');
      req.flush({ error: errorMessage }, { status: 500, statusText: 'Server Error' });
    });
  });

  describe('getInvitesByFromId', () => {
    it('should return invites for a given fromId', () => {
      const fromId = 1;
      const expectedInvites = mockInvitesList.filter(invite => invite.fromId === fromId);

      service.getInvitesByFromId(fromId).subscribe(invites => {
        expect(invites.length).toBe(expectedInvites.length);
        expect(invites).toEqual(expectedInvites);
      });

      const req = httpMock.expectOne(`${baseUrl}/from/${fromId}`);
      expect(req.request.method).toBe('GET');
      req.flush(expectedInvites);
    });

    it('should handle error when fetching invites by fromId', () => {
      const fromId = 1;
      const errorMessage = 'Error fetching invites';

      service.getInvitesByFromId(fromId).subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: Error) => {
          expect(error.message).toBe(errorMessage);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/from/${fromId}`);
      expect(req.request.method).toBe('GET');
      req.flush({ error: errorMessage }, { status: 500, statusText: 'Server Error' });
    });
  });

  describe('checkInviteRequired', () => {
    it('should return true if _requiresInvite is true', () => {
      service.checkInviteRequired().subscribe(isRequired => {
        expect(isRequired).toBeTrue();
      });

      const req = httpMock.expectOne(`${baseUrl}/is-invite-only`);
      expect(req.request.method).toBe('GET');
      req.flush({ _requiresInvite: true });
      expect(console.log).toHaveBeenCalled();
    });

    it('should return false if _requiresInvite is false', () => {
      service.checkInviteRequired().subscribe(isRequired => {
        expect(isRequired).toBeFalse();
      });

      const req = httpMock.expectOne(`${baseUrl}/is-invite-only`);
      expect(req.request.method).toBe('GET');
      req.flush({ _requiresInvite: false });
      expect(console.log).toHaveBeenCalled();
    });

    it('should return true and log error if the HTTP call fails', () => {
      const mockError = new HttpErrorResponse({
        error: 'Server down',
        status: 500,
        statusText: 'Internal Server Error'
      });

      service.checkInviteRequired().subscribe(isRequired => {
        expect(isRequired).toBeTrue(); // Should default to true on error
      });

      const req = httpMock.expectOne(`${baseUrl}/is-invite-only`);
      expect(req.request.method).toBe('GET');
      req.flush(null, mockError);

      expect(console.error).toHaveBeenCalledWith('Error in checkInviteRequired:', jasmine.any(HttpErrorResponse));
    });
  });
});
