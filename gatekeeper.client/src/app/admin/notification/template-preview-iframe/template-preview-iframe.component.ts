import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Component({
  selector: 'app-template-preview-iframe',
  templateUrl: './template-preview-iframe.component.html',
  styleUrls: ['./template-preview-iframe.component.scss'],
  standalone: false
})
export class TemplatePreviewIframeComponent implements OnChanges {
  @Input() htmlContent: string = '';
  sanitizedSrcdoc: SafeHtml = '';

  constructor(private sanitizer: DomSanitizer) { }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['htmlContent']) {
      this.updateSrcdoc();
    }
  }

  private updateSrcdoc(): void {
    // Use DomSanitizer to bypass security and trust the HTML content
    this.sanitizedSrcdoc = this.sanitizer.bypassSecurityTrustHtml(this.htmlContent);
  }
}
