import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of, throwError, BehaviorSubject } from 'rxjs';

import { UserLoginComponent } from './user-login.component';
import { AuthService } from '../../../core/services/user/auth.service';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';

describe('UserLoginComponent', () => { // Changed describe name to UserLoginComponent
  let component: UserLoginComponent;
  let fixture: ComponentFixture<UserLoginComponent>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let currentUserSubject: BehaviorSubject<any>; // Allow 'any' for user type in tests

  beforeEach(async () => {
    currentUserSubject = new BehaviorSubject(null); // Default to no user logged in
    mockAuthService = jasmine.createSpyObj('AuthService', ['login'], { currentUser$: currentUserSubject.asObservable() });
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      declarations: [UserLoginComponent],
      imports: [
        ReactiveFormsModule,
        CommonModule // Import CommonModule if your template uses things like ngIf, ngFor etc.
      ],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UserLoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // This calls ngOnInit and sets up the form
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    expect(component.loginForm).toBeDefined();
  });

  it('should not submit if form is invalid', () => {
    component.loginForm.controls['identifier'].setValue('');
    component.loginForm.controls['password'].setValue('');
    component.onSubmit();
    expect(mockAuthService.login).not.toHaveBeenCalled();
  });

  it('should call authService.login and navigate to /portal on successful login', fakeAsync(() => {
    component.loginForm.controls['identifier'].setValue('testuser');
    component.loginForm.controls['password'].setValue('password');

    mockAuthService.login.and.returnValue(of({ token: 'fake-token' })); // Mock successful login response

    component.onSubmit();
    tick(); // Simulate the passage of time for async operations like Observables

    expect(mockAuthService.login).toHaveBeenCalledWith('testuser', 'password');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/portal']);
    expect(component.errorMessage).toBe('');
  }));

  it('should display specific lockout message on 429 error with error.error.Message', fakeAsync(() => {
    component.loginForm.controls['identifier'].setValue('testuser');
    component.loginF.error.Message', fakeAsync(() => {
+    component.loginForm.controls['identifier'].setValue('testuser');
+    component.loginForm.controls['password'].setValue('password');
+
+    const errorResponse = new HttpErrorResponse({
+      status: 429,
+      error: { Message: 'Account locked. Try again in 5 minutes.' }
+    });
+    mockAuthService.login.and.returnValue(throwError(() => errorResponse));
+
+    component.onSubmit();
+    tick();
+
+    expect(mockAuthService.login).toHaveBeenCalledWith('testuser', 'password');
+    expect(component.errorMessage).toBe('Account locked. Try again in 5 minutes.');
+    expect(mockRouter.navigate).not.toHaveBeenCalled();
+  }));
+
+  it('should display default lockout message on 429 error if error.error.Message is missing', fakeAsync(() => {
+    component.loginForm.controls['identifier'].setValue('testuser');
+    component.loginForm.controls['password'].setValue('password');
+
+    // Simulate a 429 error where the 'Message' might be in a different structure or missing
+    const errorResponse = new HttpErrorResponse({
+      status: 429,
+      error: { someOtherErrorProperty: 'details' } // No 'Message' property here
+    });
+    mockAuthService.login.and.returnValue(throwError(() => errorResponse));
+
+    component.onSubmit();
+    tick();
+
+    expect(mockAuthService.login).toHaveBeenCalledWith('testuser', 'password');
+    expect(component.errorMessage).toBe('You have exceeded the maximum number of login attempts. Please try again after some time.');
+    expect(mockRouter.navigate).not.toHaveBeenCalled();
+  }));
+
+  it('should display generic error message on other error statuses (e.g., 401)', fakeAsync(() => {
+    component.loginForm.controls['identifier'].setValue('testuser');
+    component.loginForm.controls['password'].setValue('password');
+
+    const errorResponse = new HttpErrorResponse({
+      status: 401, // Simulate a generic unauthorized error
+      error: { Message: 'Invalid credentials' } // Server might still send a message
+    });
+    mockAuthService.login.and.returnValue(throwError(() => errorResponse));
+
+    component.onSubmit();
+    tick();
+
+    expect(mockAuthService.login).toHaveBeenCalledWith('testuser', 'password');
+    expect(component.errorMessage).toBe('Login failed. Please check your username and password and try again.');
+    expect(mockRouter.navigate).not.toHaveBeenCalled();
+  }));
+
+   it('should navigate to /portal if user is already logged in (currentUser$ emits)', fakeAsync(() => {
+    currentUserSubject.next({ id: 1, username: 'test' }); // Simulate user being logged in
+    tick(); // Allow subscription to process
+    fixture.detectChanges(); // Re-run change detection if needed after async operation
+
+    // This test primarily verifies the constructor/ngOnInit logic for redirection
+    // If the component is created and user is already logged in, it should navigate.
+    // Note: This specific check might be more of an integration test of the constructor logic.
+    // For UserLoginComponent, the primary navigation happens AFTER successful login,
+    // but the constructor does have a subscription to currentUser$.
+    expect(mockRouter.navigate).toHaveBeenCalledWith(['/portal']);
+  }));
+
+});
