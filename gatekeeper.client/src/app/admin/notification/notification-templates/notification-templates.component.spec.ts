import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core'; // Added for CUSTOM_ELEMENTS_SCHEMA

import { NotificationTemplatesComponent } from './notification-templates.component';

describe('NotificationTemplatesComponent', () => {
  let component: NotificationTemplatesComponent;
  let fixture: ComponentFixture<NotificationTemplatesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [NotificationTemplatesComponent],
      imports: [
        HttpClientTestingModule,
        ReactiveFormsModule,
        MatCardModule,
        MatListModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatSlideToggleModule,
        MatButtonModule,
        MatSnackBarModule,
        NoopAnimationsModule
      ],
      schemas: [CUSTOM_ELEMENTS_SCHEMA] // Added CUSTOM_ELEMENTS_SCHEMA
    })
    .compileComponents();

    fixture = TestBed.createComponent(NotificationTemplatesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
