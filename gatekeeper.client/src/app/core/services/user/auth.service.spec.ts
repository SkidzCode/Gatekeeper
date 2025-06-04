import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http'; // Corrected import
import { AuthService } from './auth.service';
// AuthResponse is an interface in AuthService.ts, use a mock or define here.
// For User, adjust path:
import { User } from '../../../shared/models/user.model';
import { Setting } from '../../../shared/models/setting.model';

// Define AuthResponse structure based on AuthService.ts
interface MockAuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
  settings: Setting[]; // Assuming Setting model might be needed
  sessionId: string;
}

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let getItemSpy: jasmine.Spy;
  let setItemSpy: jasmine.Spy;
  let removeItemSpy: jasmine.Spy;

  const mockUser: User = {
    id: 1,
    username: 'testuser', // Corrected to username
    email: 'test@example.com',
    roles: ['user'],
    // emailConfirmed removed as it's not in User model
    // Add other mandatory User fields if any, e.g., firstName, lastName
    firstName: 'Test',
    lastName: 'User',
    phone: '123-456-7890', // Added phone as it's in User model
    isActive: true         // Added isActive as it's in User model
  };

  const mockAuthResponse: MockAuthResponse = {
    accessToken: 'fake-access-token',
    refreshToken: 'fake-refresh-token',
    user: mockUser,
    settings: [], // Assuming empty settings for now
    sessionId: 'fake-session-id'
  };

  beforeEach(() => {
    getItemSpy = spyOn(localStorage, 'getItem').and.callFake(() => null);
    setItemSpy = spyOn(localStorage, 'setItem').and.callFake(() => {});
    removeItemSpy = spyOn(localStorage, 'removeItem').and.callFake(() => {});

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService] // No LocalStorageService mock needed
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);

    // Initialize service's internal state by calling its own methods that read from localStorage
    // This simulates the constructor's behavior or direct calls in a real app scenario.
    // However, AuthService constructor itself calls getUser(), getAccessToken() which use the spies.
    // So, spies must be set up BEFORE TestBed.inject(AuthService).
  });

  afterEach(() => {
    httpMock.verify(); // Verify that no unmatched requests are outstanding.
    // No need to reset localStorageServiceMock
    // localStorage spies are reset automatically by Jasmine between tests if created with spyOn in beforeEach.
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('login', () => {
    const testEmail = 'test@example.com';
    const testPassword = 'password';

    it('should send a POST request to "api/Authentication/login" and store tokens on successful login', () => {
      service.login(testEmail, testPassword).subscribe(response => {
        // Cast to MockAuthResponse as service.login returns AuthResponse from service file
        expect(response as MockAuthResponse).toEqual(mockAuthResponse);
      });

      const req = httpMock.expectOne('/api/Authentication/login');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ identifier: testEmail, password: testPassword });
      req.flush(mockAuthResponse);

      expect(setItemSpy).toHaveBeenCalledWith('accessToken', mockAuthResponse.accessToken);
      expect(setItemSpy).toHaveBeenCalledWith('refreshToken', mockAuthResponse.refreshToken);
      expect(setItemSpy).toHaveBeenCalledWith('sessionId', mockAuthResponse.sessionId);
      expect(setItemSpy).toHaveBeenCalledWith('currentUser', JSON.stringify(mockAuthResponse.user));
      expect(setItemSpy).toHaveBeenCalledWith('currentSettings', JSON.stringify(mockAuthResponse.settings));

      expect(service.getAccessToken()).toBe(mockAuthResponse.accessToken);
      expect(service.getUser()).toEqual(mockAuthResponse.user);
    });

    it('should handle login error and not store tokens', () => {
      const errorResponse = { status: 401, statusText: 'Unauthorized' };
      let actualError: any;

      service.login(testEmail, testPassword).subscribe({
        next: () => fail('should have failed with the 401 error'),
        error: (err) => actualError = err
      });

      const req = httpMock.expectOne('/api/Authentication/login');
      expect(req.request.method).toBe('POST');
      req.flush({ message: 'Invalid credentials' }, errorResponse);

      // setItemSpy for tokens should not have been called after initial setup if error occurs
      // Count calls precisely if needed, or check specific token calls are absent.
      // For simplicity, check that current state reflects no login:
      expect(service.getAccessToken()).toBeNull(); // Assumes getItemSpy returns null for 'accessToken'
      expect(service.getUser()).toBeNull(); // Assumes getItemSpy returns null for 'currentUser'
      expect(actualError).toBeTruthy();
      // The error returned by handleError is the HttpErrorResponse itself
      expect(actualError instanceof HttpErrorResponse).toBeTrue();
      expect(actualError.status).toBe(401);
    }); // Closes it('should handle login error...')
  }); // Closes describe('login')

  describe('logout', () => {
    it('should call logoutCurrentSession, clear tokens and user on logout', () => {
      // Simulate a logged-in state
      getItemSpy.withArgs('accessToken').and.returnValue(mockAuthResponse.accessToken);
      getItemSpy.withArgs('refreshToken').and.returnValue(mockAuthResponse.refreshToken);
      service = TestBed.inject(AuthService); // Re-inject to pick up new getItem state for constructor

      // Spy on logoutCurrentSession which is called by logout()
      // Since logoutCurrentSession is private, we test the public logout() effects.
      // We expect logout() to eventually call clearTokens and clearUser.

      service.logout(); // This is void

      // logout() calls logoutCurrentSession().subscribe().
      // The .tap() and .catchError() in logoutCurrentSession handle clearing.
      const req = httpMock.expectOne('/api/Authentication/logout');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ token: mockAuthResponse.refreshToken });
      req.flush({ message: 'Logged out successfully' }); // Simulate successful API response

      expect(removeItemSpy).toHaveBeenCalledWith('accessToken');
      expect(removeItemSpy).toHaveBeenCalledWith('refreshToken');
      // removeItemSpy is called twice for 'refreshToken' in clearTokens(), once for 'accessToken' and once for 'refreshToken'.
      // This is a bug in AuthService's clearTokens: localStorage.removeItem('refreshToken'); is repeated.
      // The test will reflect this actual behavior.
      expect(removeItemSpy.calls.allArgs()).toContain(['refreshToken']);
      expect(removeItemSpy).toHaveBeenCalledWith('currentUser');
      // expect(removeItemSpy).toHaveBeenCalledWith('sessionId'); // sessionId is NOT cleared by clearTokens

      expect(service.getAccessToken()).toBeNull();
      expect(service.getUser()).toBeNull();
    });

    it('should clear tokens and user even if logout API call fails', () => {
      getItemSpy.withArgs('accessToken').and.returnValue(mockAuthResponse.accessToken);
      getItemSpy.withArgs('refreshToken').and.returnValue(mockAuthResponse.refreshToken);
      service = TestBed.inject(AuthService);

      service.logout();

      const req = httpMock.expectOne('/api/Authentication/logout');
      req.flush({ message: 'Logout failed' }, { status: 500, statusText: 'Server Error' });

      expect(removeItemSpy).toHaveBeenCalledWith('accessToken');
      expect(removeItemSpy).toHaveBeenCalledWith('refreshToken');
      expect(removeItemSpy).toHaveBeenCalledWith('currentUser');
      expect(service.getAccessToken()).toBeNull();
      expect(service.getUser()).toBeNull();
    });
  });

  describe('refreshToken', () => {
    const newMockAuthResponse: MockAuthResponse = {
      accessToken: 'new-fake-access-token',
      refreshToken: 'new-fake-refresh-token',
      user: mockUser, // Assuming user details don't change on refresh for this test
      settings: [],
      sessionId: 'new-fake-session-id' // Session ID might also be refreshed
    };

    it('should send a POST request to "api/Authentication/refresh-token" and update tokens on success', () => {
      getItemSpy.withArgs('refreshToken').and.returnValue(mockAuthResponse.refreshToken);
      // service.loadInitialAuthState(); // Not needed, getItemSpy handles initial state.
      service = TestBed.inject(AuthService);


      service.refreshToken().subscribe(response => {
        expect(response.accessToken).toBe(newMockAuthResponse.accessToken);
      });

      const req = httpMock.expectOne('/api/Authentication/refresh-token');
      expect(req.request.method).toBe('POST');
      // The actual service sends { refreshToken: string }
      expect(req.request.body).toEqual({ refreshToken: mockAuthResponse.refreshToken });

      // The actual service's AuthResponse for refresh does not include user/settings,
      // but the tap operator in refreshToken() calls setTokens, setUser, setSettings.
      // This means the flush below should match the actual API response,
      // and then we test that setTokens etc. were called with the new data.
      // The AuthResponse interface in AuthService.ts is for the whole object.
      // Let's assume the refresh endpoint returns the full AuthResponse for now.
      req.flush(newMockAuthResponse);

      expect(setItemSpy).toHaveBeenCalledWith('accessToken', newMockAuthResponse.accessToken);
      expect(setItemSpy).toHaveBeenCalledWith('refreshToken', newMockAuthResponse.refreshToken);
      expect(setItemSpy).toHaveBeenCalledWith('sessionId', newMockAuthResponse.sessionId);
      // User and settings might be updated if the refresh token response includes them
      expect(setItemSpy).toHaveBeenCalledWith('currentUser', JSON.stringify(newMockAuthResponse.user));
      expect(setItemSpy).toHaveBeenCalledWith('currentSettings', JSON.stringify(newMockAuthResponse.settings));
      expect(service.getAccessToken()).toBe(newMockAuthResponse.accessToken);
    });

    it('should handle refresh token failure (e.g. API error)', () => {
      getItemSpy.withArgs('refreshToken').and.returnValue(mockAuthResponse.refreshToken);
      getItemSpy.withArgs('accessToken').and.returnValue(mockAuthResponse.accessToken); // User initially logged in
      service = TestBed.inject(AuthService);

      expect(service.getAccessToken()).toBeTruthy(); // Pre-condition

      service.refreshToken().subscribe({
        next: () => fail('should have failed to refresh token'),
        error: (err: HttpErrorResponse) => {
          expect(err).toBeTruthy();
          expect(err instanceof HttpErrorResponse).toBeTrue();
        }
      });

      const req = httpMock.expectOne('/api/Authentication/refresh-token');
      req.flush({ message: 'Invalid refresh token' }, { status: 401, statusText: 'Unauthorized' });

      // In case of refresh failure, the service does not automatically logout/clear tokens.
      // This behavior might need review in the AuthService itself.
      // Based on current AuthService.refreshToken, it just lets the error propagate.
      // So, tokens would NOT be cleared automatically by refreshToken() itself on error.
      // Let's verify this:
      expect(removeItemSpy).not.toHaveBeenCalledWith('accessToken'); // No automatic logout
      expect(service.getAccessToken()).toBe(mockAuthResponse.accessToken); // Token still there
    });

    it('should return an error observable if no refresh token is present in storage', (done) => {
      getItemSpy.withArgs('refreshToken').and.returnValue(null);
      service = TestBed.inject(AuthService); // Re-inject to pick up this specific spy state

      service.refreshToken().subscribe({
        next: () => fail('should not have attempted refresh and emitted value'),
        error: (errMessage) => { // Error is string 'No refresh token stored.'
          expect(errMessage).toBe('No refresh token stored.');
          done();
        }
      });
      httpMock.expectNone('/api/Authentication/refresh-token');
      expect(service.getAccessToken()).toBeNull(); // No change to access token status
    });
  });

  // This is the start of the corrected Getters and Observables section
  describe('getters', () => {
    it('getAccessToken should return token from localStorage', () => {
      getItemSpy.withArgs('accessToken').and.returnValue('test-token');
      expect(service.getAccessToken()).toBe('test-token');
    });

    it('getRefreshToken should return token from localStorage', () => {
      getItemSpy.withArgs('refreshToken').and.returnValue('test-refresh-token');
      expect(service.getRefreshToken()).toBe('test-refresh-token');
    });

    it('getSessionId should return session ID from localStorage', () => {
      getItemSpy.withArgs('sessionId').and.returnValue('test-session-id');
      expect(service.getSessionId()).toBe('test-session-id');
    });

    it('getUser should return user from localStorage', () => {
      getItemSpy.withArgs('currentUser').and.returnValue(JSON.stringify(mockUser));
      expect(service.getUser()).toEqual(mockUser);
    });

    it('getSettings should return settings from localStorage', () => {
      const originalDate = new Date(); // Use a fixed date for consistent string representation
      const mockSettings: Setting[] = [{
        id: 1,
        name: 'setting1',
        settingValue: 'val1', // Corrected to settingValue
        // domain, type, subType removed as they are not in Setting model
        settingValueType: 'string',
        defaultSettingValue: 'default',
        createdBy: 1,
        updatedBy: 1,
        createdAt: originalDate, // Still a Date object here
        updatedAt: originalDate  // Still a Date object here
        // parentId, userId, category are optional
      }];

      // This is what getItem will return from localStorage (dates as ISO strings)
      getItemSpy.withArgs('currentSettings').and.returnValue(JSON.stringify(mockSettings));

      const retrievedSettings = service.getSettings();

      // Create an expected object that mirrors what JSON.parse would produce from mockSettings
      const expectedSettings = mockSettings.map(setting => ({
        ...setting,
        createdAt: originalDate.toISOString(), // Compare with ISO string
        updatedAt: originalDate.toISOString()  // Compare with ISO string
      }));

      expect(retrievedSettings).toEqual(expectedSettings);
    });

    it('getRoles (derived from getUser) should return user roles if user is available', () => {
      getItemSpy.withArgs('currentUser').and.returnValue(JSON.stringify(mockUser));
      let user = service.getUser();
      expect(user?.roles).toEqual(mockUser.roles);

      getItemSpy.withArgs('currentUser').and.returnValue(null);
      user = service.getUser();
      expect(user?.roles).toBeUndefined();

      const userWithoutRoles = { ...mockUser, roles: undefined };
      getItemSpy.withArgs('currentUser').and.returnValue(JSON.stringify(userWithoutRoles));
      user = service.getUser();
      expect(user?.roles).toBeUndefined();

      const userWithEmptyRoles = { ...mockUser, roles: [] };
      getItemSpy.withArgs('currentUser').and.returnValue(JSON.stringify(userWithEmptyRoles));
      user = service.getUser();
      expect(user?.roles).toEqual([]);
    });
  });

  describe('Observables (currentAccessToken$, currentUser$, currentSettings$)', () => {
    it('currentAccessToken$ should emit token from localStorage on init, after login, and null after logout', (done) => {
      const testEmail = 'test@example.com';
      const testPassword = 'password';
      let emissions = 0;

      // Service is injected in global beforeEach, where getItemSpy for 'accessToken' returns null.
      // So, currentAccessToken$ initially emits null.
      service.currentAccessToken$.subscribe((token: string | null) => {
        emissions++;
        if (emissions === 1) {
          expect(token).toBeNull(); // Initial value from BehaviorSubject
        } else if (emissions === 2) {
          expect(token).toBe(mockAuthResponse.accessToken); // After login
        } else if (emissions === 3) {
          expect(token).toBeNull(); // After logout
          done();
        }
      });

      // Login
      service.login(testEmail, testPassword).subscribe();
      const loginReq = httpMock.expectOne('/api/Authentication/login');
      loginReq.flush(mockAuthResponse);

      // Logout
      getItemSpy.withArgs('refreshToken').and.returnValue(mockAuthResponse.refreshToken);
      service.logout();
      const logoutReq = httpMock.expectOne('/api/Authentication/logout');
      logoutReq.flush({ message: 'ok' });
    });

    it('currentUser$ should emit user from localStorage on init, after login, and null after logout', (done) => {
      const testEmail = 'test@example.com';
      const testPassword = 'password';
      let emissions = 0;

      const anotherMockUser: User = {...mockUser, id: 2, username: 'anotherUser', email: 'another@example.com', firstName: 'Ano', lastName: 'Ther'}; // Corrected to username
      const loginResponse: MockAuthResponse = {...mockAuthResponse, user: anotherMockUser, accessToken: 'newAccess', refreshToken: 'newRefresh', sessionId: 'newSession'};

      // Service is injected in global beforeEach, where getItemSpy for 'currentUser' returns null.
      // So, currentUser$ initially emits null.
      service.currentUser$.subscribe((user: User | null) => {
        emissions++;
        if (emissions === 1) {
          expect(user).toBeNull(); // Initial value
        } else if (emissions === 2) {
          expect(user).toEqual(anotherMockUser); // After login
        } else if (emissions === 3) {
          expect(user).toBeNull(); // After logout
          done();
        }
      });

      service.login(testEmail, testPassword).subscribe();
      const loginReq = httpMock.expectOne('/api/Authentication/login');
      loginReq.flush(loginResponse);

      getItemSpy.withArgs('refreshToken').and.returnValue(loginResponse.refreshToken);
      service.logout();
      const logoutReq = httpMock.expectOne('/api/Authentication/logout');
      logoutReq.flush({ message: 'ok' });
    });
  });
});
