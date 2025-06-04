import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AdminSettingsEditComponent } from './admin-settings-edit.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { FormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { SettingsService } from '../../../core/services/site/settings.service';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatRadioModule } from '@angular/material/radio';
import { MatButtonModule } from '@angular/material/button';

describe('AdminSettingsEditComponent', () => {
  let component: AdminSettingsEditComponent;
  let fixture: ComponentFixture<AdminSettingsEditComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AdminSettingsEditComponent],
      imports: [
        HttpClientTestingModule,
        RouterTestingModule,
        FormsModule,
        NoopAnimationsModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatRadioModule,
        MatButtonModule,
        MatSnackBarModule
      ],
      providers: [
        SettingsService,
        MatSnackBar,
        {
          provide: ActivatedRoute,
          useValue: {
            params: of({ id: '1' })
          }
        }
      ]
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
