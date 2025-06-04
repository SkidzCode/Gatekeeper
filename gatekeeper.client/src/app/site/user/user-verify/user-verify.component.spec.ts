import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UserVerifyComponent } from './user-verify.component';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { AuthService } from '../../../core/services/user/auth.service';

describe('UserVerifyComponent', () => {
  let component: UserVerifyComponent;
  let fixture: ComponentFixture<UserVerifyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [UserVerifyComponent],
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        {
          provide: ActivatedRoute,
          useValue: {
            queryParams: of({ token: 'mock-token' })
          }
        }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UserVerifyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
