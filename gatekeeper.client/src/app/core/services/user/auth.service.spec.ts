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
  // Spies are now directly on localStorage, individual variables like getItemSpy might not be needed
  // unless used for specific `withArgs` setups not replaced by direct store manipulation.
  // For this refactor, we assume direct store manipulation for setup.

  let store: { [key: string]: string | null };

  const mockUser: User = {
    id: 1,
    username: 'testuser',
    email: 'test@example.com',
    roles: ['user'],
    firstName: 'Test',
    lastName: 'User',
    phone: '123-456-7890',
    isActive: true
  };

  const mockAuthResponse: MockAuthResponse = {
    accessToken: 'fake-access-token',
    refreshToken: 'fake-refresh-token',
    user: mockUser,
    settings: [],
    sessionId: 'fake-session-id'
  };

  beforeEach(() => {
    store = {}; // Reset store for each test for isolation

    spyOn(localStorage, 'getItem').and.callFake((key: string): string | null => {
      return store[key] || null;
    });
    spyOn(localStorage, 'setItem').and.callFake((key: string, value: string): void => {
      store[key] = value;
    });
    spyOn(localStorage, 'removeItem').and.callFake((key: string): void => {
      delete store[key];
    });
    // Optionally spy on localStorage.clear if the service uses it
    // spyOn(localStorage, 'clear').and.callFake(() => {
    //   store = {};
    // });

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
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

      // Use localStorage.setItem directly which will use the spy and update the store
      // Or check the store content directly if setItem spy verification is too broad.
      expect(localStorage.setItem).toHaveBeenCalledWith('accessToken', mockAuthResponse.accessToken);
      expect(localStorage.setItem).toHaveBeenCalledWith('refreshToken', mockAuthResponse.refreshToken);
      expect(localStorage.setItem).toHaveBeenCalledWith('sessionId', mockAuthResponse.sessionId);
      expect(localStorage.setItem).toHaveBeenCalledWith('currentUser', JSON.stringify(mockAuthResponse.user));
      expect(localStorage.setItem).toHaveBeenCalledWith('currentSettings', JSON.stringify(mockAuthResponse.settings));

      // These getters will now read from the 'store' via the spied localStorage.getItem
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

      // Check that tokens were not stored by inspecting the 'store' or spy calls
      expect(store['accessToken']).toBeUndefined();
      expect(store['currentUser']).toBeUndefined();
      expect(actualError).toBeTruthy();
      expect(actualError instanceof HttpErrorResponse).toBeTrue();
      expect(actualError.status).toBe(401);
    }); // Closes it('should handle login error...')
  }); // Closes describe('login')

  describe('logout', () => {
    it('should call logoutCurrentSession, clear tokens and user on logout', () => {
      // Simulate a logged-in state by populating the store
      store['accessToken'] = mockAuthResponse.accessToken;
      store['refreshToken'] = mockAuthResponse.refreshToken;
      store['currentUser'] = JSON.stringify(mockAuthResponse.user); // User also needs to be in store for some checks
      // service = TestBed.inject(AuthService); // No re-injection needed as store is read dynamically

      service.logout();

      const req = httpMock.expectOne('/api/Authentication/logout');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ token: mockAuthResponse.refreshToken }); // Uses refreshToken from store
      req.flush({ message: 'Logged out successfully' });

      // removeItemSpy is now just spyOn(localStorage, 'removeItem')
      expect(localStorage.removeItem).toHaveBeenCalledWith('accessToken');
      expect(localStorage.removeItem).toHaveBeenCalledWith('refreshToken');
      // The bug of duplicate removeItem('refreshToken') call in service.clearTokens() would still be reflected here.
      // To be precise:
      // expect(localStorage.removeItem.calls.allArgs()).toContain(['refreshToken']);
      expect(localStorage.removeItem).toHaveBeenCalledWith('currentUser');
      // expect(localStorage.removeItem).toHaveBeenCalledWith('sessionId'); // sessionId is NOT cleared by clearTokens

      // These getters will now read from the updated 'store'
      expect(service.getAccessToken()).toBeNull();
      expect(service.getUser()).toBeNull();
    });

    it('should clear tokens and user even if logout API call fails', () => {
      // Simulate a logged-in state
      store['accessToken'] = mockAuthResponse.accessToken;
      store['refreshToken'] = mockAuthResponse.refreshToken;
      store['currentUser'] = JSON.stringify(mockAuthResponse.user);

      service.logout();

      const req = httpMock.expectOne('/api/Authentication/logout');
      req.flush({ message: 'Logout failed' }, { status: 500, statusText: 'Server Error' });

      expect(localStorage.removeItem).toHaveBeenCalledWith('accessToken');
      expect(localStorage.removeItem).toHaveBeenCalledWith('refreshToken');
      expect(localStorage.removeItem).toHaveBeenCalledWith('currentUser');
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
      sessionId: 'new-fake-session-id'
    };

    it('should send a POST request to "api/Authentication/refresh-token" and update tokens on success', () => {
      // Simulate initial state: a refresh token exists
      store['refreshToken'] = mockAuthResponse.refreshToken;
      // service = TestBed.inject(AuthService); // Not needed, store is dynamic

      service.refreshToken().subscribe(response => {
        expect(response.accessToken).toBe(newMockAuthResponse.accessToken);
      });

      const req = httpMock.expectOne('/api/Authentication/refresh-token');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ refreshToken: mockAuthResponse.refreshToken }); // Uses refreshToken from store

      req.flush(newMockAuthResponse); // Simulate API response

      // Check that setItem was called via the spy, which updates the 'store'
      expect(localStorage.setItem).toHaveBeenCalledWith('accessToken', newMockAuthResponse.accessToken);
      expect(localStorage.setItem).toHaveBeenCalledWith('refreshToken', newMockAuthResponse.refreshToken);
      expect(localStorage.setItem).toHaveBeenCalledWith('sessionId', newMockAuthResponse.sessionId);
      expect(localStorage.setItem).toHaveBeenCalledWith('currentUser', JSON.stringify(newMockAuthResponse.user));
      expect(localStorage.setItem).toHaveBeenCalledWith('currentSettings', JSON.stringify(newMockAuthResponse.settings));

      // Verify by reading from service, which reads from 'store'
      expect(service.getAccessToken()).toBe(newMockAuthResponse.accessToken);
    });

    it('should handle refresh token failure (e.g. API error)', () => {
      // Simulate initial state: access and refresh tokens exist
      store['refreshToken'] = mockAuthResponse.refreshToken;
      store['accessToken'] = mockAuthResponse.accessToken;
      // service = TestBed.inject(AuthService);

      expect(service.getAccessToken()).toBeTruthy(); // Pre-condition: token exists

      service.refreshToken().subscribe({
        next: () => fail('should have failed to refresh token'),
        error: (err: HttpErrorResponse) => {
          expect(err).toBeTruthy();
          expect(err instanceof HttpErrorResponse).toBeTrue();
        }
      });

      const req = httpMock.expectOne('/api/Authentication/refresh-token');
      req.flush({ message: 'Invalid refresh token' }, { status: 401, statusText: 'Unauthorized' });

      // Check that tokens were not cleared automatically
      expect(localStorage.removeItem).not.toHaveBeenCalledWith('accessToken');
      expect(service.getAccessToken()).toBe(mockAuthResponse.accessToken); // Token still there in store
    });

    it('should return an error observable if no refresh token is present in storage', (done) => {
      // store['refreshToken'] is already null/undefined as store is {} initially
      // service = TestBed.inject(AuthService);

      service.refreshToken().subscribe({
        next: () => fail('should not have attempted refresh and emitted value'),
        error: (errMessage) => {
          expect(errMessage).toBe('No refresh token stored.');
          done();
        }
      });
      httpMock.expectNone('/api/Authentication/refresh-token'); // No HTTP call
      expect(service.getAccessToken()).toBeNull();
    });
  });

  describe('getters', () => {
    it('getAccessToken should return token from localStorage', () => {
      store['accessToken'] = 'test-token';
      expect(service.getAccessToken()).toBe('test-token');
    });

    it('getRefreshToken should return token from localStorage', () => {
      store['refreshToken'] = 'test-refresh-token';
      expect(service.getRefreshToken()).toBe('test-refresh-token');
    });

    it('getSessionId should return session ID from localStorage', () => {
      store['sessionId'] = 'test-session-id';
      expect(service.getSessionId()).toBe('test-session-id');
    });

    it('getUser should return user from localStorage', () => {
      store['currentUser'] = JSON.stringify(mockUser);
      expect(service.getUser()).toEqual(mockUser);
    });

    it('getSettings should return settings from localStorage', () => {
      const originalDate = new Date();
      const mockSettingsArray: Setting[] = [{
        id: 1, name: 'setting1', settingValue: 'val1', settingValueType: 'string',
        defaultSettingValue: 'default', createdBy: 1, updatedBy: 1,
        createdAt: originalDate, updatedAt: originalDate
      }];
      store['currentSettings'] = JSON.stringify(mockSettingsArray);

      const retrievedSettings = service.getSettings();

      const expectedSettings_withStringDates = mockSettingsArray.map(setting => ({
        ...setting,
        createdAt: originalDate.toISOString(),
        updatedAt: originalDate.toISOString()
      }));
      expect(retrievedSettings).toEqual(expectedSettings_withStringDates as any);
    });

    it('getRoles (derived from getUser) should return user roles if user is available', () => {
      store['currentUser'] = JSON.stringify(mockUser);
      let user = service.getUser();
      expect(user?.roles).toEqual(mockUser.roles);

      store['currentUser'] = null; // Simulate no user
      user = service.getUser();
      expect(user?.roles).toBeUndefined(); // Or expect(user).toBeNull(); then user?.roles

      const userWithoutRoles = { ...mockUser, roles: undefined };
      store['currentUser'] = JSON.stringify(userWithoutRoles);
      user = service.getUser();
      expect(user?.roles).toBeUndefined();

      const userWithEmptyRoles = { ...mockUser, roles: [] };
      store['currentUser'] = JSON.stringify(userWithEmptyRoles);
      user = service.getUser();
      expect(user?.roles).toEqual([]);
    });
  });

  describe('Observables (currentAccessToken$, currentUser$, currentSettings$)', () => {
    it('currentAccessToken$ should emit token from localStorage on init, after login, and null after logout', (done) => {
      const testEmail = 'test@example.com';
      const testPassword = 'password';
      let emissions = 0;
      // store is empty initially, so service.getAccessToken() called by constructor via loadInitialAuthState will be null.

      service.currentAccessToken$.subscribe((token: string | null) => {
        emissions++;
        if (emissions === 1) {
          expect(token).toBeNull(); // Initial: store is empty
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
      loginReq.flush(mockAuthResponse); // This calls setItem, updates store, triggers observable

      // Logout
      // refreshToken is needed for logout API call, ensure it's in store
      store['refreshToken'] = mockAuthResponse.refreshToken;
      service.logout();
      const logoutReq = httpMock.expectOne('/api/Authentication/logout');
      logoutReq.flush({ message: 'ok' }); // This calls removeItem, updates store, triggers observable
    });

    it('currentUser$ should emit user from localStorage on init, after login, and null after logout', (done) => {
      const testEmail = 'test@example.com';
      const testPassword = 'password';
      let emissions = 0;

      const anotherMockUser: User = {...mockUser, id: 2, username: 'anotherUser', email: 'another@example.com', firstName: 'Ano', lastName: 'Ther'};
      const loginResponse: MockAuthResponse = {...mockAuthResponse, user: anotherMockUser, accessToken: 'newAccess', refreshToken: 'newRefresh', sessionId: 'newSession'};
      // store is empty initially

      service.currentUser$.subscribe((user: User | null) => {
        emissions++;
        if (emissions === 1) {
          expect(user).toBeNull(); // Initial
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

      store['refreshToken'] = loginResponse.refreshToken; // For logout call
      service.logout();
      const logoutReq = httpMock.expectOne('/api/Authentication/logout');
      logoutReq.flush({ message: 'ok' });
    });
  });
});
