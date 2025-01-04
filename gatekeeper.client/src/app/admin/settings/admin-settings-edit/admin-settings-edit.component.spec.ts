import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminSettingsEditComponent } from './admin-settings-edit.component';

describe('AdminSettingsEditComponent', () => {
  let component: AdminSettingsEditComponent;
  let fixture: ComponentFixture<AdminSettingsEditComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AdminSettingsEditComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminSettingsEditComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
