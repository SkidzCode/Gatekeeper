import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { AdminLogsBrowserComponent } from './admin-logs-browser.component';

describe('AdminLogsBrowserComponent', () => {
  let component: AdminLogsBrowserComponent;
  let fixture: ComponentFixture<AdminLogsBrowserComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AdminLogsBrowserComponent],
      imports: [HttpClientTestingModule]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminLogsBrowserComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
