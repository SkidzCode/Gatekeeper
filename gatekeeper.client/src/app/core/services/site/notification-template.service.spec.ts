import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';

import { NotificationTemplateService } from './notification-template.service';
// Assuming NotificationTemplate model path - will verify later by reading service and model file
import { NotificationTemplate } from '../../../shared/models/notification.template.model';

describe('NotificationTemplateService', () => {
  let service: NotificationTemplateService;
  let httpMock: HttpTestingController;

  // Base API URL - verified from service source
  const baseUrl = '/api/NotificationTemplate';

  // Mock data - adjusted based on actual NotificationTemplate model
  const mockTemplate: NotificationTemplate = {
    templateId: 1,
    templateName: 'Test Template', // Corrected from name
    subject: 'Subject: {{name}}',
    body: 'Hello {{name}}, this is a test.',
    isActive: true, // Corrected from isDefault, added isActive
    channel: 'email',
    tokenType: "Token", // Was present, confirmed by model
    createdAt: new Date().toISOString(), // Optional, but good for mock consistency
    updatedAt: new Date().toISOString()  // Optional
  };

  const mockTemplates: NotificationTemplate[] = [
    mockTemplate,
    {
      templateId: 2,
      templateName: 'Another Template', // Corrected
      subject: 'Welcome {{username}}',
      body: 'Welcome aboard, {{username}}!',
      isActive: false, // Corrected
      channel: 'push',
      tokenType: "Token2",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [NotificationTemplateService]
    });
    service = TestBed.inject(NotificationTemplateService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Ensure no outstanding requests
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  // Placeholder for more tests

  // Tests for getAllNotificationTemplates
  describe('getAllNotificationTemplates', () => {
    it('should retrieve all notification templates via GET request', () => {
      service.getAllNotificationTemplates().subscribe(templates => {
        expect(templates.length).toBe(2);
        expect(templates).toEqual(mockTemplates);
      });

      const req = httpMock.expectOne(baseUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockTemplates);
    });

    it('should handle errors for getAllNotificationTemplates', () => {
      service.getAllNotificationTemplates().subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(500);
          // Further error message checking can be done if service's handleError is more specific
        }
      });

      const req = httpMock.expectOne(baseUrl);
      req.flush({ message: 'Error fetching templates' }, { status: 500, statusText: 'Server Error' });
    });
  });

  // Tests for getNotificationTemplateById
  describe('getNotificationTemplateById', () => {
    it('should retrieve a specific template by ID via GET request', () => {
      const templateId = 1;
      service.getNotificationTemplateById(templateId).subscribe(template => {
        expect(template).toEqual(mockTemplate);
      });

      const req = httpMock.expectOne(`${baseUrl}/${templateId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockTemplate);
    });

    it('should handle errors when template not found (404) for getNotificationTemplateById', () => {
      const templateId = 999; // Non-existent ID
      service.getNotificationTemplateById(templateId).subscribe({
        next: () => fail('should have failed with 404 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(404);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${templateId}`);
      req.flush({ message: 'Template not found' }, { status: 404, statusText: 'Not Found' });
    });
  });

  // Tests for createNotificationTemplate
  describe('createNotificationTemplate', () => {
    // Payload for create uses Omit<NotificationTemplate, 'templateId' | 'createdAt' | 'updatedAt'>
    const newTemplatePayload: Omit<NotificationTemplate, 'templateId' | 'createdAt' | 'updatedAt'> = {
      templateName: 'New Awesome Template',
      channel: 'sms',
      tokenType: 'twilio',
      subject: 'SMS Subject: {{data}}',
      body: 'Your SMS body: {{data}}',
      isActive: true
    };
    const expectedCreateResponse = { message: 'Template created successfully', templateId: 3 };

    it('should send new template data via POST request', () => {
      service.createNotificationTemplate(newTemplatePayload).subscribe(response => {
        expect(response).toEqual(expectedCreateResponse);
      });

      const req = httpMock.expectOne(baseUrl); // POST to baseUrl
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newTemplatePayload);
      req.flush(expectedCreateResponse);
    });

    it('should handle validation errors (400) for createNotificationTemplate', () => {
      const invalidPayload: any = { ...newTemplatePayload, templateName: '' }; // Example: empty name
      service.createNotificationTemplate(invalidPayload).subscribe({
        next: () => fail('should have failed with a 400 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(400);
        }
      });

      const req = httpMock.expectOne(baseUrl);
      req.flush({ error: 'Validation failed', errors: { templateName: ['Name is required']}}, { status: 400, statusText: 'Bad Request' });
    });
  });

  // Tests for updateNotificationTemplate
  describe('updateNotificationTemplate', () => {
    const templateToUpdate: NotificationTemplate = {
      ...mockTemplate, // Use existing mock as base
      templateName: 'Updated Template Name',
      isActive: false
    };
    const expectedUpdateResponse = { message: 'Template updated successfully' };

    it('should send updated template data via PUT request', () => {
      // Ensure templateId is present for update
      if (templateToUpdate.templateId === undefined) {
        fail('templateId must be defined for update tests');
        return;
      }

      service.updateNotificationTemplate(templateToUpdate).subscribe(response => {
        expect(response).toEqual(expectedUpdateResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/${templateToUpdate.templateId}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(templateToUpdate);
      req.flush(expectedUpdateResponse);
    });

    it('should handle errors if template to update is not found (404)', () => {
      const nonExistentTemplate: NotificationTemplate = { ...templateToUpdate, templateId: 999 };
      service.updateNotificationTemplate(nonExistentTemplate).subscribe({
        next: () => fail('should have failed with 404 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(404);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${nonExistentTemplate.templateId}`);
      req.flush({ message: 'Template not found for update' }, { status: 404, statusText: 'Not Found' });
    });
  });

  // Tests for deleteNotificationTemplate
  describe('deleteNotificationTemplate', () => {
    const templateIdToDelete = 1;
    const expectedDeleteResponse = { message: 'Template deleted successfully' };

    it('should send a DELETE request with the template ID', () => {
      service.deleteNotificationTemplate(templateIdToDelete).subscribe(response => {
        expect(response).toEqual(expectedDeleteResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/${templateIdToDelete}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(expectedDeleteResponse);
    });

    it('should handle errors if template to delete is not found (404)', () => {
      const nonExistentTemplateId = 999;
      service.deleteNotificationTemplate(nonExistentTemplateId).subscribe({
        next: () => fail('should have failed with 404 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(404);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${nonExistentTemplateId}`);
      req.flush({ message: 'Template not found for delete' }, { status: 404, statusText: 'Not Found' });
    });

    it('should handle server errors (500) during delete', () => {
      const templateIdWithError = 500; // Using ID to signify an error condition for this test
      service.deleteNotificationTemplate(templateIdWithError).subscribe({
        next: () => fail('should have failed with 500 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(500);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${templateIdWithError}`);
      req.flush({ message: 'Error during deletion' }, { status: 500, statusText: 'Server Error' });
    });
  });
});
