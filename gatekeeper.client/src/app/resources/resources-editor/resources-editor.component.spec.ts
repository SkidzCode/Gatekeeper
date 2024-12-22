import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ResourcesEditorComponent } from './resources-editor.component';

describe('ResourcesEditorComponent', () => {
  let component: ResourcesEditorComponent;
  let fixture: ComponentFixture<ResourcesEditorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ResourcesEditorComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ResourcesEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
