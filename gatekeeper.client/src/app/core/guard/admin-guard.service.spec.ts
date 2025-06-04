import { TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { AdminGuard } from './admin-guard.service';
import { AuthService } from '../services/user/auth.service';
import { User } from '../../shared/models/user.model'; // Corrected path

import { of, BehaviorSubject } from 'rxjs'; // Import 'of' and 'BehaviorSubject' for mocking observables

describe('AdminGuard', () => {
  let guard: AdminGuard;
  let authServiceMock: Pick<AuthService, 'currentUser$'>; // Mock only the parts of AuthService that AdminGuard uses
  let routerMock: jasmine.SpyObj<Router>;
  let currentUserSubject: BehaviorSubject<User | null>;

  const dummyRoute = {} as ActivatedRouteSnapshot;
  const dummyState = {} as RouterStateSnapshot;

  // Mock user objects
  const adminUser: User = {
    id: 1, username: 'admin', roles: ['Admin', 'User'],
    firstName: 'Admin', lastName: 'User', email: 'admin@example.com', phone: '', isActive: true
  };
  const regularUser: User = {
    id: 2, username: 'user', roles: ['User'],
    firstName: 'Regular', lastName: 'User', email: 'user@example.com', phone: '', isActive: true
  };

  beforeEach(() => {
    currentUserSubject = new BehaviorSubject<User | null>(null); // Initialize with null (not authenticated)
    authServiceMock = {
      currentUser$: currentUserSubject.asObservable()
    };
    routerMock = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [RouterTestingModule],
      providers: [
        AdminGuard,
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    });
    guard = TestBed.inject(AdminGuard);
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });

  it('should allow activation if user is Admin', (done) => {
    currentUserSubject.next(adminUser); // Emit admin user

    guard.canActivate(dummyRoute, dummyState).subscribe(canActivate => {
      expect(canActivate).toBeTrue();
      expect(routerMock.navigate).not.toHaveBeenCalled();
      done();
    });
  });

  it('should prevent activation and navigate to /forbidden if user is not authenticated (currentUser$ emits null)', (done) => {
    currentUserSubject.next(null); // Emit null (not authenticated)

    guard.canActivate(dummyRoute, dummyState).subscribe(canActivate => {
      expect(canActivate).toBeFalse();
      expect(routerMock.navigate).toHaveBeenCalledWith(['/forbidden']);
      done();
    });
  });

  it('should prevent activation and navigate to /forbidden if user is not Admin', (done) => {
    currentUserSubject.next(regularUser); // Emit regular user

    guard.canActivate(dummyRoute, dummyState).subscribe(canActivate => {
      expect(canActivate).toBeFalse();
      expect(routerMock.navigate).toHaveBeenCalledWith(['/forbidden']);
      done();
    });
  });

  it('should prevent activation if user object has no roles array (edge case)', (done) => {
    const userWithoutRolesAttribute: Partial<User> = { id: 3, username: 'noroles', firstName: 'No', lastName: 'Roles', email: 'nr@e.com', phone: '', isActive: true };
    // roles property is completely missing
    currentUserSubject.next(userWithoutRolesAttribute as User);


    guard.canActivate(dummyRoute, dummyState).subscribe(canActivate => {
      expect(canActivate).toBeFalse();
      expect(routerMock.navigate).toHaveBeenCalledWith(['/forbidden']);
      done();
    });
  });

  it('should prevent activation if user roles array is empty (edge case)', (done) => {
    const userWithEmptyRoles: User = {
        id: 4, username: 'emptyroles', roles: [],
        firstName: 'Empty', lastName: 'Roles', email: 'er@e.com', phone: '', isActive: true
    };
    currentUserSubject.next(userWithEmptyRoles);

    guard.canActivate(dummyRoute, dummyState).subscribe(canActivate => {
      expect(canActivate).toBeFalse();
      expect(routerMock.navigate).toHaveBeenCalledWith(['/forbidden']);
      done();
    });
  });

});
