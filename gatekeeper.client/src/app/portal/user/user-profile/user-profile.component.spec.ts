import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { UserService } from '../../../core/services/user/user.service';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

import { UserProfileComponent } from './user-profile.component';
import { User } from '../../../shared/models/user.model';

describe('UserProfileComponent', () => {
  let component: UserProfileComponent;
  let fixture: ComponentFixture<UserProfileComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [UserProfileComponent],
      imports: [
        HttpClientTestingModule,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatCardModule,
        MatIconModule,
        MatDividerModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatSnackBarModule
      ],
      providers: [
        UserService,
        MatSnackBar
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UserProfileComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController); // Add this
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify(); // Verify that no unmatched requests are outstanding
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call updateUserWithImage on saveUser and handle success', () => {
    // Mock current user for the component
    const mockUser: User = {
      id: 1, firstName: 'Test', lastName: 'User', username: 'testuser',
      email: 'test@example.com', phone: '1234567890', roles: ['User'], isActive: true
    };
    spyOn(component, 'getUser').and.returnValue(mockUser); // Mock internal getUser
    component.ngOnInit(); // Initialize form with user data
    fixture.detectChanges();

    // Verify that ngOnInit patched the form with mockUser data
    expect(component.profileForm.value.firstName).toBe(mockUser.firstName);
    expect(component.profileForm.value.email).toBe(mockUser.email);

    // Now, update the form for the saveUser test
    component.profileForm.patchValue({
      firstName: 'UpdatedFirst',
      lastName: 'UpdatedLast',
      username: 'updatedUser',
      email: 'updated@example.com',
      phone: '0987654321'
    });
    expect(component.profileForm.valid).toBeTrue();

    component.saveUser();

    const req = httpMock.expectOne('/api/User/UpdateWithImage');
    expect(req.request.method).toBe('POST');
    req.flush({ user: { ...mockUser, firstName: 'UpdatedFirst' } }); // Simulate a successful response

    // Check for snackbar message (optional, but good)
    // spyOn(TestBed.inject(MatSnackBar), 'open');
    // expect(TestBed.inject(MatSnackBar).open).toHaveBeenCalledWith('Profile updated successfully!', 'Close', jasmine.any(Object));

    // Verify localStorage update (optional, but good)
    // const updatedUserInStorage = JSON.parse(localStorage.getItem('currentUser') || '{}');
    // expect(updatedUserInStorage.firstName).toBe('UpdatedFirst');
  });
});
