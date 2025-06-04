import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';

import { UserService } from './user.service';
import { User } from '../../../shared/models/user.model'; // Adjusted path based on previous experience
import { Role } from '../../../shared/models/role.model'; // Assuming Role model exists and path

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  // Base API URL from UserService
  const baseUrl = '/api/User'; // Corrected baseUrl

  const mockUser: User = {
    id: 1,
    username: 'testuser',
    firstName: 'Test',
    lastName: 'User',
    email: 'test@example.com',
    phone: '123-456-7890',
    isActive: true,
    roles: ['user']
  };

  const mockUsers: User[] = [
    mockUser,
    {
      id: 2, username: 'anotheruser', firstName: 'Another', lastName: 'User',
      email: 'another@example.com', phone: '987-654-3210', isActive: false, roles: ['admin', 'user']
    }
  ];

  const mockRoles: Role[] = [
    { id: 1, roleName: 'user' },
    { id: 2, roleName: 'admin' }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [UserService]
    });
    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Ensure no outstanding requests
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  // Tests for getUsers
  describe('getUsers', () => {
    it('should retrieve all users via GET request', () => {
      service.getUsers().subscribe(users => {
        expect(users.length).toBe(2);
        expect(users).toEqual(mockUsers);
      });

      const req = httpMock.expectOne(`${baseUrl}/users`); // Corrected endpoint
      expect(req.request.method).toBe('GET');
      req.flush(mockUsers);
    });

    it('should handle errors for getUsers', () => {
      const errorMessage = 'Error fetching users';
      service.getUsers().subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: string) => { // Changed type to string
          expect(error).toBe(errorMessage); // Assert the string message
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/users`); // Corrected endpoint
      // Ensure the flushed error body matches what handleError expects for error.error.error
      req.flush({ error: errorMessage }, { status: 500, statusText: 'Server Error' });
    });
  });

  // More tests will be added here

  // Tests for getProfile
  describe('getProfile', () => {
    it('should retrieve the current user profile via GET request', () => {
      service.getProfile().subscribe(user => {
        expect(user).toEqual(mockUser);
      });

      const req = httpMock.expectOne(`${baseUrl}/profile`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUser);
    });

    it('should handle errors for getProfile', () => {
      const errorMessage = 'Error fetching profile';
      service.getProfile().subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: string) => { // handleError returns a string
          expect(error).toContain('Error fetching profile'); // Check if the custom message or part of it is there
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/profile`);
      // Simulate a server error response
      req.flush({ error: errorMessage }, { status: 500, statusText: 'Server Error' });
    });
  });

  // Tests for getUserById
  describe('getUserById', () => {
    const userId = 1;
    it('should retrieve a user by ID via GET request', () => {
      service.getUserById(userId).subscribe(user => {
        expect(user).toEqual(mockUser);
      });

      const req = httpMock.expectOne(`${baseUrl}/user/${userId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUser);
    });

    it('should handle errors for getUserById when user not found (404)', () => {
      const nonExistentUserId = 999;
      const errorMessage = 'User not found';
      service.getUserById(nonExistentUserId).subscribe({
        next: () => fail('should have failed with 404 error'),
        error: (error: string) => {
          expect(error).toContain('User not found');
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/user/${nonExistentUserId}`);
      req.flush({ error: errorMessage }, { status: 404, statusText: 'Not Found' });
    });
  });

  // Tests for getUserByIdEdit
  describe('getUserByIdEdit', () => {
    const userId = 1;
    const mockUserEdit = {
      user: mockUser,
      roles: mockRoles
    };

    it('should retrieve user edit data by ID via GET request', () => {
      service.getUserByIdEdit(userId).subscribe(userEditData => {
        expect(userEditData).toEqual(mockUserEdit);
      });

      const req = httpMock.expectOne(`${baseUrl}/user/edit/${userId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUserEdit);
    });

    it('should handle errors for getUserByIdEdit', () => {
      const nonExistentUserId = 999;
      const errorMessage = 'Cannot get user edit data';
      service.getUserByIdEdit(nonExistentUserId).subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: string) => {
          expect(error).toContain('Cannot get user edit data');
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/user/edit/${nonExistentUserId}`);
      req.flush({ error: errorMessage }, { status: 500, statusText: 'Server Error' });
    });
  });

  // Tests for updateUser
  describe('updateUser', () => {
    const updatedUser: User = { ...mockUser, firstName: 'UpdatedFirstName' };

    it('should send user data via POST request for update', () => {
      const expectedResponse = { message: 'User updated successfully' };
      service.updateUser(updatedUser).subscribe(response => {
        expect(response).toEqual(expectedResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/Update`);
      expect(req.request.method).toBe('POST');
      // The service constructs a payload. Let's match that.
      const expectedPayload = {
        id: updatedUser.id,
        firstName: updatedUser.firstName,
        lastName: updatedUser.lastName,
        email: updatedUser.email,
        username: updatedUser.username,
        phone: updatedUser.phone,
        roles: updatedUser.roles
      };
      expect(req.request.body).toEqual(expectedPayload);
      req.flush(expectedResponse);
    });

    it('should handle server-side errors for updateUser', () => {
      const serverErrorMessage = 'Update failed due to server validation';
      service.updateUser(updatedUser).subscribe({
        next: () => fail('should have failed with a server error'),
        error: (error: Error) => { // updateUser's catchError returns throwError(() => new Error(errorMessage))
          expect(error.message).toContain('Server-side error: Status: 400'); // Check part of the constructed message
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/Update`);
      // Simulate a server error response (e.g., validation error)
      req.flush({ errors: { username: ['Username already taken'] } }, { status: 400, statusText: 'Bad Request' });
    });

    it('should handle client-side/network errors for updateUser', () => {
      const networkError = new ErrorEvent('Network error', {
        message: 'Simulated network error'
      });
      service.updateUser(updatedUser).subscribe({
        next: () => fail('should have failed with a network error'),
        error: (error: Error) => {
          expect(error.message).toContain('Client-side error: Simulated network error');
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/Update`);
      req.error(networkError);
    });
  });

  // Tests for updateUserWithImage
  describe('updateUserWithImage', () => {
    it('should send FormData via POST request for updateWithImage', () => {
      const mockFormData = new FormData();
      mockFormData.append('userId', mockUser.id.toString());
      // Create a dummy file
      const dummyFile = new File(['dummy content'], 'example.png', { type: 'image/png' });
      mockFormData.append('imageFile', dummyFile);

      const expectedResponse = { user: mockUser }; // Assuming API returns the updated user

      service.updateUserWithImage(mockFormData).subscribe(response => {
        expect(response).toEqual(expectedResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/UpdateWithImage`);
      expect(req.request.method).toBe('POST');
      // FormData is tricky to compare directly. Check for its presence.
      expect(req.request.body instanceof FormData).toBeTrue();
      // You could also check specific fields if necessary, though it's more complex:
      // expect(req.request.body.get('userId')).toBe(mockUser.id.toString());
      req.flush(expectedResponse);
    });

    it('should handle errors for updateUserWithImage', () => {
      const mockFormData = new FormData();
      mockFormData.append('userId', 'invalid_data_that_causes_error');

      const errorMessage = 'Image update failed';
      // Note: updateUserWithImage uses the generic handleError from the service.
      service.updateUserWithImage(mockFormData).subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: string) => {
          expect(error).toContain('Image update failed');
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/UpdateWithImage`);
      req.flush({ error: errorMessage }, { status: 500, statusText: 'Server Error' });
    });
  });
});
