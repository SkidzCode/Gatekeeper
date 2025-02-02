import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-theme-switch',
  standalone: true,
  imports: [CommonModule, MatSlideToggleModule, MatButtonModule, MatIconModule],
  templateUrl: 'theme-switch.component.html',
  styleUrls: ['theme-switch.component.scss']
})
export class ThemeSwitchComponent implements OnInit {
  isLightTheme = false; // default to dark theme

  ngOnInit() {
    // Check if there's a saved theme in localStorage
    const savedTheme = localStorage.getItem('isLightTheme');
    if (savedTheme !== null) {
      this.isLightTheme = JSON.parse(savedTheme);
    }
    this.updateTheme();
  }

  toggleTheme() {
    this.isLightTheme = !this.isLightTheme;
    // Save the new theme state in localStorage
    localStorage.setItem('isLightTheme', JSON.stringify(this.isLightTheme));
    this.updateTheme();
  }

  updateTheme() {
    const htmlElement = document.documentElement;
    htmlElement.classList.remove('light', 'dark');
    htmlElement.classList.add(this.isLightTheme ? 'light' : 'dark');
  }
}
