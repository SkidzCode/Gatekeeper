import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, convertToParamMap, ParamMap, Params } from '@angular/router';
import { of, Observable, throwError, BehaviorSubject } from 'rxjs';
import { CommonModule } from '@angular/common';

// Angular Material Modules
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
// Removed CUSTOM_ELEMENTS_SCHEMA to ensure template elements are correctly recognized

import { UserRegisterComponent } from './user-register.component';
import { AuthService } from '../../../core/services/user/auth.service';
import { InviteService } from '../../../core/services/user/invite.service';
import { WindowRef } from '../../../core/services/utils/window-ref.service';
import { NotificationService } from '../../../core/services/site/notification.service';

// Mock classes for services
class MockAuthService {
  register = jasmine.createSpy('register').and.returnValue(of({ message: 'Success' }));
  checkEmailTaken = jasmine.createSpy('checkEmailTaken').and.returnValue(of(false));
  checkUsernameTaken = jasmine.createSpy('checkUsernameTaken').and.returnValue(of(false));
  validatePasswordStrength = jasmine.createSpy('validatePasswordStrength').and.returnValue(of({ isValid: true }));
}

class MockInviteService {
  checkInviteRequired = jasmine.createSpy('checkInviteRequired').and.returnValue(of(false));
}

class MockRouter {
  navigate = jasmine.createSpy('navigate');
}

class MockActivatedRoute {
  private queryParamsSource = new BehaviorSubject<Params>({ token: 'test-token' }); // Default with token
  public queryParams = this.queryParamsSource.asObservable();

  setParams(params: Params) {
    this.queryParamsSource.next(params);
  }
}

class MockWindowRef {
  nativeWindow = { location: { origin: 'http://mock-origin.com' } };
}

class MockNotificationService {
  showSuccess = jasmine.createSpy('showSuccess');
  showError = jasmine.createSpy('showError');
}

describe('UserRegisterComponent', () => {
  let component: UserRegisterComponent;
  let fixture: ComponentFixture<UserRegisterComponent>;
  let authService: MockAuthService;
  let inviteService: MockInviteService;
  let router: MockRouter;
  let mockActivatedRoute: MockActivatedRoute;

  beforeEach(async () => {
    mockActivatedRoute = new MockActivatedRoute(); // Create instance for all tests in this describe

    await TestBed.configureTestingModule({
      declarations: [UserRegisterComponent],
      imports: [
        ReactiveFormsModule,
        FormsModule,
        CommonModule,
        NoopAnimationsModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatCheckboxModule,
        MatIconModule
      ],
      providers: [
        { provide: AuthService, useClass: MockAuthService },
        { provide: InviteService, useClass: MockInviteService },
        { provide: Router, useClass: MockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }, // Use the same instance
        { provide: WindowRef, useClass: MockWindowRef },
        { provide: NotificationService, useClass: MockNotificationService }
      ]
      // schemas: [CUSTOM_ELEMENTS_SCHEMA] // Removed to ensure template elements are correctly recognized
    }).compileComponents();

    fixture = TestBed.createComponent(UserRegisterComponent);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService) as unknown as MockAuthService;
    inviteService = TestBed.inject(InviteService) as unknown as MockInviteService;
    router = TestBed.inject(Router) as unknown as MockRouter;
    // mockActivatedRoute instance is already available from the outer scope
  });

  it('should create', fakeAsync(() => {
    mockActivatedRoute.setParams({ token: 'test-token' }); // Ensure default for this test
    fixture.detectChanges();
    tick();
    expect(component).toBeTruthy();
  }));

  describe('ngOnInit', () => {
    it('should initialize the form', fakeAsync(() => {
      mockActivatedRoute.setParams({ token: 'test-token' });
      fixture.detectChanges();
      tick();
      expect(component.registerForm).toBeDefined();
      expect(component.registerForm.get('firstName')).toBeDefined();
      // ... (other form fields)
    }));

    it('should set token from ActivatedRoute queryParams (token present)', fakeAsync(() => {
      mockActivatedRoute.setParams({ token: 'test-token' });
      fixture.detectChanges();
      tick();
      expect(component.token).toBe('test-token');
    }));

    it('should set token to empty if not in queryParams', fakeAsync(() => {
      mockActivatedRoute.setParams({});
      fixture.detectChanges();
      tick();
      expect(component.token).toBe('');
    }));

    it('should set inviteOnly to false if token is present and invite required by service', fakeAsync(() => {
      mockActivatedRoute.setParams({ token: 'test-token' });
      inviteService.checkInviteRequired.and.returnValue(of(true));
      fixture.detectChanges();
      tick();
      expect(inviteService.checkInviteRequired).toHaveBeenCalled();
      expect(component.inviteOnly).toBeFalse();
    }));

    it('should set inviteOnly to false if invite not required by service (token present)', fakeAsync(() => {
      mockActivatedRoute.setParams({ token: 'test-token' });
      inviteService.checkInviteRequired.and.returnValue(of(false));
      fixture.detectChanges();
      tick();
      expect(inviteService.checkInviteRequired).toHaveBeenCalled();
      expect(component.inviteOnly).toBeFalse();
    }));

    it('should set inviteOnly to true if invite required by service and no token', fakeAsync(() => {
      mockActivatedRoute.setParams({});
      inviteService.checkInviteRequired.and.returnValue(of(true));
      fixture.detectChanges();
      tick();
      expect(inviteService.checkInviteRequired).toHaveBeenCalled();
      expect(component.inviteOnly).toBeTrue();
    }));

    it('should set inviteOnly to false if invite not required by service and no token', fakeAsync(() => {
      mockActivatedRoute.setParams({});
      inviteService.checkInviteRequired.and.returnValue(of(false));
      fixture.detectChanges();
      tick();
      expect(inviteService.checkInviteRequired).toHaveBeenCalled();
      expect(component.inviteOnly).toBeFalse();
    }));

    it('should set inviteOnly to true if checkInviteRequired errors and no token', fakeAsync(() => {
      mockActivatedRoute.setParams({});
      inviteService.checkInviteRequired.and.returnValue(throwError(() => new Error('API error')));
      fixture.detectChanges();
      tick();
      expect(inviteService.checkInviteRequired).toHaveBeenCalled();
      expect(component.inviteOnly).toBeTrue();
    }));

    it('should subscribe to newPassword valueChanges', fakeAsync(() => {
      mockActivatedRoute.setParams({ token: 'test-token' });
      fixture.detectChanges();
      tick(500);
      expect(component.passwordSubscription).toBeDefined();
      expect(component.passwordSubscription?.closed).toBeFalse();
      component.ngOnDestroy();
      expect(component.passwordSubscription?.closed).toBeTrue();
    }));
  });

  // Form validation tests would go here, similar to the previous structure
  // but adapted to this new beforeEach setup.

  describe('Form Validations', () => {
    // These tests use the component instance created in the main beforeEach.
    // We'll call fixture.detectChanges() and tick() within each relevant validation test's fakeAsync block
    // after setting control values.

    beforeEach(fakeAsync(() => {
      // Ensure a default state for ActivatedRoute for these validation tests if needed,
      // though most control validations are independent of route params.
      // Form is initialized after first detectChanges in 'it' block or a more specific beforeEach.
      // Let's ensure form is created before each validation test.
      mockActivatedRoute.setParams({ token: 'test-token' }); // Default setup for validation context
      fixture.detectChanges(); // Create form by calling ngOnInit
      tick(); // Settle ngOnInit async calls
    }));

    describe('firstName control', () => {
      it('should be invalid when empty (required validator)', () => {
        const control = component.registerForm.get('firstName');
        control?.setValue('');
        expect(control?.hasError('required')).toBeTrue();
      });

      it('should be invalid if less than 2 characters (minlength validator)', () => {
        const control = component.registerForm.get('firstName');
        control?.setValue('A');
        expect(control?.hasError('minlength')).toBeTrue();
      });

      it('should be valid if 2 or more characters', () => {
        const control = component.registerForm.get('firstName');
        control?.setValue('Ab');
        expect(control?.valid).toBeTrue();
      });
    });

    describe('lastName control', () => {
      it('should be invalid when empty (required validator)', () => {
        const control = component.registerForm.get('lastName');
        control?.setValue('');
        expect(control?.hasError('required')).toBeTrue();
      });

      it('should be invalid if less than 2 characters (minlength validator)', () => {
        const control = component.registerForm.get('lastName');
        control?.setValue('B');
        expect(control?.hasError('minlength')).toBeTrue();
      });

      it('should be valid if 2 or more characters', () => {
        const control = component.registerForm.get('lastName');
        control?.setValue('Bc');
        expect(control?.valid).toBeTrue();
      });
    });

    describe('email control', () => {
      it('should be invalid when empty (required validator)', () => {
        const control = component.registerForm.get('email');
        control?.setValue('');
        expect(control?.hasError('required')).toBeTrue();
      });

      it('should be invalid for incorrect email format (email validator)', () => {
        const control = component.registerForm.get('email');
        control?.setValue('invalid-email');
        expect(control?.hasError('email')).toBeTrue();
      });

      it('should be valid for correct email format (before async check)', fakeAsync(() => {
        const control = component.registerForm.get('email');
        control?.setValue('valid@example.com');
        // Sync validators pass
        expect(control?.hasError('required')).toBeFalse();
        expect(control?.hasError('email')).toBeFalse();
        tick(500); // Allow async to run, even if it's 'not taken'
      }));

      it('should be invalid if email is taken (async validator)', fakeAsync(() => {
        const control = component.registerForm.get('email');
        authService.checkEmailTaken.and.returnValue(of(true));
        control?.setValue('taken@example.com');
        tick(500);
        expect(control?.hasError('emailTaken')).toBeTrue();
      }));

      it('should be valid if email is not taken (async validator)', fakeAsync(() => {
        const control = component.registerForm.get('email');
        authService.checkEmailTaken.and.returnValue(of(false));
        control?.setValue('new@example.com');
        tick(500);
        expect(control?.hasError('emailTaken')).toBeFalse();
        expect(control?.valid).toBeTrue();
      }));

      it('should be valid if email check service fails (async catchError)', fakeAsync(() => {
        const control = component.registerForm.get('email');
        authService.checkEmailTaken.and.returnValue(throwError(() => new Error('API Error')));
        control?.setValue('any@example.com');
        tick(500);
        expect(control?.hasError('emailTaken')).toBeFalse();
        expect(control?.valid).toBeTrue();
      }));
    });

    describe('username control', () => {
      it('should be invalid when empty (required validator)', () => {
        const control = component.registerForm.get('username');
        control?.setValue('');
        expect(control?.hasError('required')).toBeTrue();
      });

      it('should be invalid if less than 4 characters (minlength validator)', () => {
        const control = component.registerForm.get('username');
        control?.setValue('usr');
        expect(control?.hasError('minlength')).toBeTrue();
      });

      it('should be valid if 4 or more characters (before async check)', fakeAsync(() => {
        const control = component.registerForm.get('username');
        control?.setValue('user');
        expect(control?.hasError('required')).toBeFalse();
        expect(control?.hasError('minlength')).toBeFalse();
        tick(500); // Allow async to run
      }));

      it('should be invalid if username is taken (async validator)', fakeAsync(() => {
        const control = component.registerForm.get('username');
        authService.checkUsernameTaken.and.returnValue(of(true));
        control?.setValue('takenuser');
        tick(500);
        expect(control?.hasError('usernameTaken')).toBeTrue();
      }));

      it('should be valid if username is not taken (async validator)', fakeAsync(() => {
        const control = component.registerForm.get('username');
        authService.checkUsernameTaken.and.returnValue(of(false));
        control?.setValue('newuser');
        tick(500);
        expect(control?.hasError('usernameTaken')).toBeFalse();
        expect(control?.valid).toBeTrue();
      }));

      it('should be valid if username check service fails (async catchError)', fakeAsync(() => {
        const control = component.registerForm.get('username');
        authService.checkUsernameTaken.and.returnValue(throwError(() => new Error('API Error')));
        control?.setValue('anyuser');
        tick(500);
        expect(control?.hasError('usernameTaken')).toBeFalse();
        expect(control?.valid).toBeTrue();
      }));
    });

    describe('phone control', () => {
      it('should be invalid when empty (required validator)', () => {
        const control = component.registerForm.get('phone');
        control?.setValue('');
        expect(control?.hasError('required')).toBeTrue();
      });

      it('should match pattern (10-15 digits)', () => {
        const control = component.registerForm.get('phone');
        control?.setValue('123');
        expect(control?.hasError('pattern')).toBeTrue();
        control?.setValue('1234567890');
        expect(control?.valid).toBeTrue();
        control?.setValue('123456789012345');
        expect(control?.valid).toBeTrue();
        control?.setValue('1234567890123456');
        expect(control?.hasError('pattern')).toBeTrue();
        control?.setValue('abcdefghij');
        expect(control?.hasError('pattern')).toBeTrue();
      });
    });

    describe('newPassword control', () => {
      it('should be invalid when empty (required validator)', () => {
        const control = component.registerForm.get('newPassword');
        control?.setValue('');
        expect(control?.hasError('required')).toBeTrue();
      });

      it('should be invalid if less than 8 characters (minlength validator)', () => {
        const control = component.registerForm.get('newPassword');
        control?.setValue('short');
        expect(control?.hasError('minlength')).toBeTrue();
      });

      it('should be valid regarding sync validators if 8 or more characters', fakeAsync(() => {
        const control = component.registerForm.get('newPassword');
        control?.setValue('longenough');
        expect(control?.hasError('required')).toBeFalse();
        expect(control?.hasError('minlength')).toBeFalse();
        tick(500); // for async password strength
      }));

      it('should be invalid if password strength is weak (async validator)', fakeAsync(() => {
        const control = component.registerForm.get('newPassword');
        authService.validatePasswordStrength.and.returnValue(of({ isValid: false }));
        control?.setValue('weakpassword');
        tick(500);
        expect(control?.hasError('weakPassword')).toBeTrue();
      }));

      it('should be valid if password strength is good (async validator)', fakeAsync(() => {
        const control = component.registerForm.get('newPassword');
        authService.validatePasswordStrength.and.returnValue(of({ isValid: true }));
        control?.setValue('strongpassword123');
        tick(500);
        expect(control?.hasError('weakPassword')).toBeFalse();
        expect(control?.valid).toBeTrue();
      }));

      it('should have passwordStrengthCheckFailed error if strength service fails (async validator)', fakeAsync(() => {
        const control = component.registerForm.get('newPassword');
        authService.validatePasswordStrength.and.returnValue(throwError(() => new Error('API Error')));
        control?.setValue('anypassword123');
        tick(500);
        expect(control?.hasError('passwordStrengthCheckFailed')).toBeTrue();
      }));
    });

    describe('confirmPassword control', () => {
      it('should be invalid when empty (required validator)', () => {
        const control = component.registerForm.get('confirmPassword');
        control?.setValue('');
        expect(control?.hasError('required')).toBeTrue();
      });
    });

    describe('userLicAgreement control', () => {
      it('should be invalid if not checked (requiredTrue validator)', () => {
        const control = component.registerForm.get('userLicAgreement');
        control?.setValue(false);
        expect(control?.hasError('requiredtrue')).toBeTrue();
      });

      it('should be valid if checked', () => {
        const control = component.registerForm.get('userLicAgreement');
        control?.setValue(true);
        expect(control?.valid).toBeTrue();
      });
    });

    describe('passwordMatchValidator (form group validator)', () => {
      it('should not have passwordMismatch error if passwords match', () => {
        const form = component.registerForm;
        form.get('newPassword')?.setValue('password123');
        form.get('confirmPassword')?.setValue('password123');
        expect(form.hasError('passwordMismatch')).toBeFalse();
      });

      it('should have passwordMismatch error if passwords do not match', () => {
        const form = component.registerForm;
        form.get('newPassword')?.setValue('password123');
        form.get('confirmPassword')?.setValue('password456');
        // Trigger validation manually or through interaction if needed, though setValue should trigger it
        // For form group validators, errors are on the form group
        expect(form.hasError('passwordMismatch')).toBeTrue();
      });
    });
  });

  // TODO: Add tests for onSubmit method (success and error cases)
  // TODO: Add tests for onTokenSubmit method
  // TODO: Add tests for toggleEULA method
  // TODO: Add tests for navigateToLogin method
}); // Closes the main describe('UserRegisterComponent', ...)