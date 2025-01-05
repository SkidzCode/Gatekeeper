import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UserVerifyComponent } from './user-verify.component';

describe('UserVerifyComponent', () => {
  let component: UserVerifyComponent;
  let fixture: ComponentFixture<UserVerifyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [UserVerifyComponent]
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
