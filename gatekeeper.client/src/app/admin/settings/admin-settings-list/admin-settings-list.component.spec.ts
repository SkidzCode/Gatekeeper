import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminSettingsListComponent } from './admin-settings-list.component';

describe('AdminSettingsListComponent', () => {
  let component: AdminSettingsListComponent;
  let fixture: ComponentFixture<AdminSettingsListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AdminSettingsListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminSettingsListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
