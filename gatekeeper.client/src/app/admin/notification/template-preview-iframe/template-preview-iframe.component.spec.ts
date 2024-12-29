import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TemplatePreviewIframeComponent } from './template-preview-iframe.component';

describe('TemplatePreviewIframeComponent', () => {
  let component: TemplatePreviewIframeComponent;
  let fixture: ComponentFixture<TemplatePreviewIframeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [TemplatePreviewIframeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TemplatePreviewIframeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
