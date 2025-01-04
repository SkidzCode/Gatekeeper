import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SettingsService } from '../../../services/site/settings.service';
import { Setting } from '../../../models/setting.model';

@Component({
  selector: 'app-admin-settings-list',
  templateUrl: './admin-settings-list.component.html',
  styleUrls: ['./admin-settings-list.component.scss'],
  standalone: false
})
export class AdminSettingsListComponent implements OnInit {
  settings: Setting[] = [];
  displayedColumns: string[] = [
    'id',
    'name',
    'category',
    'type',
    'defaultValue',
    'actions'
  ];

  // Search/Filter Fields
  searchName: string = '';
  searchCategory: string = '';

  loading: boolean = false;
  errorMessage: string = '';

  constructor(
    private router: Router,
    private snackBar: MatSnackBar,
    private settingsService: SettingsService
  ) { }

  ngOnInit(): void {
    this.loadSettings();
  }

  /**
   * Load all settings, then filter out those with a userId.
   * If your backend offers a dedicated endpoint for default settings,
   * you could call that instead.
   */
  loadSettings(): void {
    this.loading = true;
    this.settingsService.getAllSettings().subscribe({
      next: (allSettings) => {
        this.loading = false;
        // Consider "default" if userId is 0, null, or undefined.
        this.settings = allSettings.filter(s => !s.userId);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = `Error loading settings: ${err}`;
      },
    });
  }

  /**
   * Perform a search using the service's searchSettings() method,
   * then filter out user-specific settings.
   */
  onSearch(): void {
    this.loading = true;
    this.settingsService
      .searchSettings(this.searchName, this.searchCategory)
      .subscribe({
        next: (results) => {
          this.loading = false;
          this.settings = results.filter(s => !s.userId);
        },
        error: (err) => {
          this.loading = false;
          this.errorMessage = `Error searching settings: ${err}`;
        },
      });
  }

  /**
   * Navigate to the edit page for a specific setting ID.
   */
  editSetting(settingId: number): void {
    this.router.navigate(['/admin', 'settings', 'edit', settingId]);
  }

  /**
   * Navigate to a "new" setting creation page.
   */
  createSetting(): void {
    this.router.navigate(['/admin', 'settings', 'new']);
  }

  /**
   * Delete a setting. Includes a confirmation prompt.
   */
  deleteSetting(setting: Setting): void {
    const confirmed = confirm(`Delete the setting "${setting.name}"?`);
    if (!confirmed) return;

    this.loading = true;
    this.settingsService.deleteSetting(setting.id).subscribe({
      next: (res) => {
        this.loading = false;
        this.snackBar.open(
          res.message || 'Setting deleted!',
          'Close',
          { duration: 3000 }
        );
        // Reload the list
        this.loadSettings();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = `Error deleting setting: ${err}`;
      },
    });
  }
}
