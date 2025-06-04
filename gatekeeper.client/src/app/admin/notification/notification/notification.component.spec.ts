import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { PageEvent } from '@angular/material/paginator'; // Import PageEvent
import { ReactiveFormsModule } from '@angular/forms';

// Import services to be mocked
import { NotificationService } from '../../../core/services/site/notification.service';
import { NotificationTemplateService } from '../../../core/services/site/notification-template.service';
import { UserService } from '../../../core/services/user/user.service';
import { AuthService } from '../../../core/services/user/auth.service';

// Import the component to be tested
import { NotificationComponent } from './notification.component';
import { NotificationSendComponent } from '../notification-send/notification-send.component';
import { TemplatePreviewIframeComponent } from '../template-preview-iframe/template-preview-iframe.component';
import { NotificationTemplatesComponent } from '../notification-templates/notification-templates.component';


// Import models - Corrected path
import { Notification } from '../../../../../src/app/shared/models/notification.model'; // Adjusted path

// Angular Material Modules
import { MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule } from '@angular/material/dialog';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card'; // Added
import { EventEmitter } from '@angular/core'; // Import EventEmitter

import { of, throwError, Subject } from 'rxjs';

describe('NotificationComponent', () => {
  let component: NotificationComponent;
  let fixture: ComponentFixture<NotificationComponent>;
  let mockNotificationService: jasmine.SpyObj<NotificationService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let paginatorTopPageEmitter: EventEmitter<PageEvent>;
  let paginatorBottomPageEmitter: EventEmitter<PageEvent>;


  const mockNotificationsData: Notification[] = [
    { id: 1, recipientId: 101, channel: 'email', subject: 'Sub1', message: 'Msg1', tokenType: 'type1', createdAt: new Date().toISOString(), isSent: false },
    { id: 2, recipientId: 102, channel: 'sms', subject: 'Sub2', message: 'Msg2', tokenType: 'type2', createdAt: new Date().toISOString(), isSent: true }
  ];

  beforeEach(waitForAsync(() => {
    mockNotificationService = jasmine.createSpyObj('NotificationService',
      ['getAllNotifications'] // Expanded based on component usage
    );
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);
    mockDialog = jasmine.createSpyObj('MatDialog', ['open']);

    TestBed.configureTestingModule({
      declarations: [
        NotificationComponent,
        NotificationSendComponent,
        TemplatePreviewIframeComponent,
        NotificationTemplatesComponent
      ],
      imports: [
        HttpClientTestingModule,
        NoopAnimationsModule,
        MatTableModule,
        MatPaginatorModule,
        MatSnackBarModule,
        MatDialogModule,
        MatTabsModule, // Added for tab change
        MatCardModule, // Added
        ReactiveFormsModule
      ],
      providers: [
        { provide: NotificationService, useValue: mockNotificationService },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MatDialog, useValue: mockDialog },
        NotificationTemplateService,
        UserService,
        AuthService
      ]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NotificationComponent);
    component = fixture.componentInstance;

    paginatorTopPageEmitter = new EventEmitter<PageEvent>();
    paginatorBottomPageEmitter = new EventEmitter<PageEvent>();

    // Mock paginators before fixture.detectChanges() / ngOnInit
    component.paginatorTop = {
      page: paginatorTopPageEmitter, // Use EventEmitter directly
      pageIndex: 0,
      pageSize: 10,
      length: 0,
      _changePageSize: jasmine.createSpy('_changePageSize')
    } as Partial<MatPaginator> as MatPaginator;

    component.paginatorBottom = {
      page: paginatorBottomPageEmitter, // Use EventEmitter directly
      pageIndex: 0,
      pageSize: 10,
      length: 0
      // No need for _changePageSize spy here if not called on paginatorBottom directly
    } as Partial<MatPaginator> as MatPaginator; // Cast to Partial then to full type

    // Default mock implementations
    mockNotificationService.getAllNotifications.and.returnValue(of(mockNotificationsData));
  });

  it('should create', () => {
    fixture.detectChanges(); // Trigger ngOnInit which calls fetchNotificationLog
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('should call fetchNotificationLog on initialization', () => {
      spyOn(component, 'fetchNotificationLog');
      fixture.detectChanges(); // Calls ngOnInit
      expect(component.fetchNotificationLog).toHaveBeenCalled();
    });
  });

  describe('fetchNotificationLog', () => {
    beforeEach(() => {
      // fixture.detectChanges(); // ngOnInit already calls it, ensure clean state if needed for direct test
    });

    it('should load notifications into dataSource on successful fetch', () => {
      mockNotificationService.getAllNotifications.and.returnValue(of(mockNotificationsData));
      component.fetchNotificationLog(); // Call directly or via ngOnInit in a fresh fixture
      expect(component.dataSource.data).toEqual(mockNotificationsData);
      expect(component.loadingNotifications).toBeFalse();
    });

    it('should set loadingNotifications to false and show snackbar on error', () => {
      const errorResponse = new Error('Failed to fetch');
      mockNotificationService.getAllNotifications.and.returnValue(throwError(() => errorResponse));

      component.fetchNotificationLog();

      expect(component.loadingNotifications).toBeFalse();
      expect(mockSnackBar.open).toHaveBeenCalledWith('Error fetching notifications', 'OK', { duration: 3000 });
    });

    it('should set loadingNotifications to true while fetching', () => {
      mockNotificationService.getAllNotifications.and.returnValue(new Subject<Notification[]>()); // Keep it pending
      component.fetchNotificationLog();
      expect(component.loadingNotifications).toBeTrue();
    });
  });

  describe('ngAfterViewInit', () => {
    it('should assign paginatorTop to dataSource.paginator', () => {
      fixture.detectChanges(); // ngOnInit & ngAfterViewInit
      expect(component.dataSource.paginator).toBe(component.paginatorTop);
    });

    // More detailed tests for paginator event subscriptions can be complex and might be better for E2E.
    // For unit tests, ensuring they are assigned is often a good start.
    // To test subscription logic, you might need to emit values on the mocked page observables.
    it('should synchronize paginatorBottom when paginatorTop changes', () => {
        fixture.detectChanges(); // ngOnInit & ngAfterViewInit
        const testPageEvent: PageEvent = { pageIndex: 1, pageSize: 20, length: 100 };

        paginatorTopPageEmitter.emit(testPageEvent); // Emit event using EventEmitter

        expect(component.paginatorBottom.pageIndex).toBe(testPageEvent.pageIndex);
        expect(component.paginatorBottom.pageSize).toBe(testPageEvent.pageSize);
    });

    it('should synchronize paginatorTop when paginatorBottom changes', () => {
        fixture.detectChanges(); // ngOnInit & ngAfterViewInit
        const testPageEvent: PageEvent = { pageIndex: 2, pageSize: 5, length: 50 };

        paginatorBottomPageEmitter.emit(testPageEvent); // Emit event using EventEmitter

        expect(component.paginatorTop.pageIndex).toBe(testPageEvent.pageIndex);
        expect(component.paginatorTop.pageSize).toBe(testPageEvent.pageSize);
        expect(component.paginatorTop._changePageSize).toHaveBeenCalledWith(testPageEvent.pageSize);
    });
  });

  describe('onTabChange', () => {
    beforeEach(() => {
      fixture.detectChanges(); // Initial ngOnInit call
    });

    it('should update selectedTabIndex', () => {
      const tabChangeEvent = { index: 1 };
      component.onTabChange(tabChangeEvent);
      expect(component.selectedTabIndex).toBe(1);
    });

    it('should call fetchNotificationLog if tab index is 1 and dataSource is empty', () => {
      component.dataSource.data = []; // Ensure data is empty
      spyOn(component, 'fetchNotificationLog');

      const tabChangeEvent = { index: 1 };
      component.onTabChange(tabChangeEvent);

      expect(component.fetchNotificationLog).toHaveBeenCalled();
    });

    it('should not call fetchNotificationLog if tab index is 1 but dataSource is not empty', () => {
      component.dataSource.data = mockNotificationsData; // Data is not empty
      spyOn(component, 'fetchNotificationLog');

      const tabChangeEvent = { index: 1 };
      component.onTabChange(tabChangeEvent);

      expect(component.fetchNotificationLog).not.toHaveBeenCalled();
    });

    it('should not call fetchNotificationLog if tab index is not 1', () => {
      component.dataSource.data = [];
      spyOn(component, 'fetchNotificationLog');

      const tabChangeEvent = { index: 0 };
      component.onTabChange(tabChangeEvent);

      expect(component.fetchNotificationLog).not.toHaveBeenCalled();
    });
  });
});
