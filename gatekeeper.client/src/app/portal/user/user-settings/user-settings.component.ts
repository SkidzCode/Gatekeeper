import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, FormControl } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';

import { AuthService } from '../../../services/user/auth.service';
import { SettingsService } from '../../../services/site/settings.service';
import { Setting } from '../../../models/setting.model';

@Component({
  selector: 'app-user-settings',
  templateUrl: './user-settings.component.html',
  styleUrls: ['./user-settings.component.scss'],
  standalone: false
})
export class UserSettingsComponent implements OnInit {
  settingsForm!: FormGroup;
  settingsArray: Setting[] = [];

  loading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private settingsService: SettingsService,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit(): void {
    this.initForm();
    this.loadSettingsFromServer();
  }

  private initForm(): void {
    // Create a group with a form array named "settings"
    this.settingsForm = this.fb.group({
      settings: this.fb.array([])
    });
  }

  /**
   * Build the FormArray and convert existing string values
   * into the correct typed value for form controls.
   */
  private buildFormArray(settings: Setting[]): void {
    const formArray = this.settingsForm.get('settings') as FormArray;
    formArray.clear();

    settings.forEach(setting => {
      let val: any = setting.settingValue; // Original string from server

      // Convert from string -> typed value for form usage.
      switch (setting.settingValueType) {
        case 'boolean':
          // Convert 'true' / 'false' -> boolean
          val = (val === 'true');
          break;

        case 'integer':
          // Convert string -> number (integer)
          val = parseInt(val, 10);
          if (isNaN(val)) {
            val = 0;
          }
          break;

        case 'float':
          // Convert string -> number (float)
          val = parseFloat(val);
          if (isNaN(val)) {
            val = 0.0;
          }
          break;

        case 'json':
          // If you ever want to parse JSON here, you could do it.
          // For now, treat as raw string (possibly multiline).
          break;

        default:
          // 'string' or unknown, leave as-is
          break;
      }

      // Create form control with the typed value.
      formArray.push(new FormControl(val));
    });
  }

  private loadSettingsFromServer(): void {
    this.loading = true;
    this.settingsService.getAllSettings().subscribe({
      next: (settings) => {
        this.loading = false;
        this.settingsArray = settings;
        this.buildFormArray(settings);
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = error;
      }
    });
  }

  onSubmit(): void {
    const updatedValues = this.settingsForm.value.settings as any[]; // typed values from the form

    // Loop through original settings array and update the .settingValue
    for (let i = 0; i < this.settingsArray.length; i++) {
      const setting = this.settingsArray[i];
      let typedValue = updatedValues[i];

      // Convert from typed value -> string for saving to server
      switch (setting.settingValueType) {
        case 'boolean':
          typedValue = typedValue ? 'true' : 'false';
          break;

        case 'integer':
        case 'float':
          typedValue = typedValue.toString();
          break;

        // If you actually want to handle real JSON serialization, you could do:
        // case 'json':
        //   try {
        //     // Or store as typedValue if the user is editing the raw text
        //     JSON.parse(typedValue); // optional validation
        //   } catch (err) {
        //     console.warn('Invalid JSON entered:', err);
        //   }
        //   break;

        default:
          typedValue = typedValue ? typedValue.toString() : '';
          break;
      }

      // Assign back to the setting model
      setting.settingValue = typedValue;
    }

    this.loading = true;
    let completedRequests = 0;
    let failedRequests = 0;

    this.settingsArray.forEach((setting) => {
      this.settingsService
        .addOrUpdateSetting({
          // If there is a user-specific version, use setting.id; otherwise (default),
          // you might want to pass 0 to create a new setting with userId set
          id: (setting as any).userId ? setting.id : 0,
          name: setting.name,
          category: setting.category,
          settingValueType: setting.settingValueType,
          defaultSettingValue: setting.defaultSettingValue,
          settingValue: setting.settingValue,
          createdBy: setting.createdBy,
          updatedBy: setting.updatedBy,
          parentId: setting.parentId,
          // You might want to set the userId here if the user is customizing:
          userId: (setting as any).userId,
        })
        .subscribe({
          next: () => {
            completedRequests++;
            if (completedRequests + failedRequests === this.settingsArray.length) {
              this.handleSaveComplete(completedRequests, failedRequests);
            }
          },
          error: () => {
            failedRequests++;
            if (completedRequests + failedRequests === this.settingsArray.length) {
              this.handleSaveComplete(completedRequests, failedRequests);
            }
          },
        });
    });
  }

  private handleSaveComplete(completed: number, failed: number): void {
    this.loading = false;
    if (failed === 0) {
      this.snackBar.open('All settings saved successfully!', 'Close', {
        duration: 3000,
      });
    } else {
      this.snackBar.open(`Some settings failed to update.`, 'Close', {
        duration: 5000,
      });
    }
  }

  onCancel(): void {
    // If you want to revert to the original values in memory:
    this.buildFormArray(this.settingsArray);

    // If you want to force reload from server instead:
    // this.loadSettingsFromServer();
  }
}
