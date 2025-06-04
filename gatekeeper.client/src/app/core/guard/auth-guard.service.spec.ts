import { TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { AuthGuard } from './auth-guard.service';
import { AuthService } from '../services/user/auth.service';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let authServiceMock: jasmine.SpyObj<AuthService>;
  let routerMock: jasmine.SpyObj<Router>;

  // Dummy route and state for canActivate
  const dummyRoute = {} as ActivatedRouteSnapshot;
  const dummyState = {} as RouterStateSnapshot;

  beforeEach(() => {
    // Create spy objects for AuthService and Router
    // Need to find out the actual public methods of AuthService to mock correctly.
    // For now, assuming getAccessToken() is the way to check auth status, as per previous AuthService tests.
    // Or, if AuthService has an isAuthenticated() method, that would be simpler.
    // Let's assume for now it has currentAccessToken$ which emits a token or null.
    authServiceMock = jasmine.createSpyObj('AuthService', ['getAccessToken']);
    routerMock = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [
        RouterTestingModule // RouterTestingModule.withRoutes([]) can be used if you test specific routes
      ],
      providers: [
        AuthGuard,
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    });
    guard = TestBed.inject(AuthGuard);
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });

  it('should allow activation if user is authenticated', () => {
    authServiceMock.getAccessToken.and.returnValue('fake-token'); // User is authenticated

    const canActivate = guard.canActivate(); // Called without arguments

    expect(canActivate).toBeTrue();
    expect(authServiceMock.getAccessToken).toHaveBeenCalled();
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('should prevent activation and navigate to login if user is not authenticated', () => {
    authServiceMock.getAccessToken.and.returnValue(null); // User is not authenticated

    const canActivate = guard.canActivate(); // Called without arguments

    expect(canActivate).toBeFalse();
    expect(authServiceMock.getAccessToken).toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']); // Or whatever the guard navigates to
  });

  // Add more tests if there are other conditions or return values from canActivate
});
