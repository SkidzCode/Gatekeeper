import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Setting } from '../../../shared/models/setting.model';
import { SettingsService } from '../../../core/services/site/settings.service';


@Component({
  selector: 'app-admin-settings-edit',
  templateUrl: './admin-settings-edit.component.html',
  styleUrls: ['./admin-settings-edit.component.scss'],
  standalone: false
})
export class AdminSettingsEditComponent implements OnInit {
  settingId: number | null = null;
  isNew = false;

  // Our local Setting model
  setting: Setting = {
    id: 0,
    name: '',
    settingValueType: 'string',
    defaultSettingValue: '',
    settingValue: '',
    createdBy: 0,
    updatedBy: 0,
    createdAt: new Date(),
    updatedAt: new Date(),
    // Optional fields
    category: '',
    userId: undefined,
    parentId: undefined,
  };

  loading = false;
  errorMessage = '';

  // Allowed types in your system
  allowedValueTypes: Setting['settingValueType'][] = [
    'string',
    'integer',
    'boolean',
    'float',
    'json',
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    private settingsService: SettingsService
  ) { }

  ngOnInit(): void {
    // If the route param is present, we are editing an existing setting
    this.route.params.subscribe((params) => {
      const idParam = params['id'];
      if (!idParam) {
        this.isNew = true;
      } else {
        this.settingId = +idParam;
        this.loadSetting(this.settingId);
      }
    });
  }

  loadSetting(id: number): void {
    this.loading = true;
    this.settingsService.getSettingById(id).subscribe({
      next: (res) => {
        this.loading = false;
        this.setting = res;
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = `Error loading setting: ${err}`;
      },
    });
  }

  saveSetting(): void {
    // Validate minimal required fields:
    if (!this.setting.name?.trim()) {
      alert('Please enter a setting name.');
      return;
    }

    this.loading = true;

    if (this.isNew) {
      // Create a new setting. For default settings, userId should be undefined or 0.
      // Also note: The server may automatically set createdAt, updatedAt, etc.
      // or you can pass them if required.
      this.setting.userId = undefined;

      // The service expects something like Omit<Setting, 'id'|'createdAt'|'updatedAt'> 
      // for the "addSetting()" method. So pass only what's needed:
      this.setting.defaultSettingValue = String(this.setting.defaultSettingValue);
      this.settingsService
        .addSetting({
          parentId: this.setting.parentId,
          userId: this.setting.userId,
          name: this.setting.name,
          category: this.setting.category,
          settingValueType: this.setting.settingValueType,
          defaultSettingValue: this.setting.defaultSettingValue,
          settingValue: String(this.setting.defaultSettingValue), // For default, we typically let settingValue be empty
          createdBy: this.setting.createdBy, // or set to your current admin user ID
          updatedBy: this.setting.updatedBy, // or same
        })
        .subscribe({
          next: (res) => {
            this.loading = false;
            this.snackBar.open('New setting created!', 'Close', {
              duration: 3000,
            });
            this.router.navigate(['/admin', 'settings']);
          },
          error: (err) => {
            this.loading = false;
            this.errorMessage = `Error creating setting: ${err}`;
          },
        });
    } else {
      // Update existing setting
      // We pass the full Setting object, but typically the server 
      // might ignore fields like createdAt, updatedAt, etc.
      // userId should remain unchanged if it's a default setting.
      this.setting.defaultSettingValue = String(this.setting.defaultSettingValue);
      this.setting.settingValue = String(this.setting.settingValue);
      this.settingsService.updateSetting(this.setting).subscribe({
        next: (res) => {
          this.loading = false;
          this.snackBar.open('Setting updated successfully!', 'Close', {
            duration: 3000,
          });
          this.router.navigate(['/admin', 'settings']);
        },
        error: (err) => {
          this.loading = false;
          this.errorMessage = `Error updating setting: ${err}`;
        },
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/admin', 'settings']);
  }
}
