import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DisabledComponent } from './disabled.component';

describe('DisabledComponent', () => {
  let component: DisabledComponent;
  let fixture: ComponentFixture<DisabledComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [DisabledComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DisabledComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
