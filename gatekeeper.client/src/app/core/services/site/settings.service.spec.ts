import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';

import { SettingsService } from './settings.service';
// Assuming Setting model path - will verify later by reading service and model file
import { Setting } from '../../../shared/models/setting.model';

describe('SettingsService', () => {
  let service: SettingsService;
  let httpMock: HttpTestingController;

  // Base API URL - corrected from service source
  const baseUrl = '/api/Settings';

  // Mock data - adjusted based on actual Setting model
  const mockSetting: Setting = {
    id: 1,
    name: 'SiteName',
    settingValue: 'Gatekeeper Portal',
    category: 'General', // Added category
    settingValueType: 'string',
    defaultSettingValue: 'Default Portal Value', // Clarified field
    createdBy: 1, // Assuming a user ID
    updatedBy: 1, // Assuming a user ID
    createdAt: new Date(), // Changed to Date object
    updatedAt: new Date()  // Changed to Date object
    // parentId and userId are optional
  };

  const mockSettings: Setting[] = [
    mockSetting,
    {
      id: 2,
      name: 'MaintenanceMode',
      settingValue: 'false',
      category: 'General',
      settingValueType: 'boolean',
      defaultSettingValue: 'false',
      createdBy: 1,
      updatedBy: 1,
      createdAt: new Date(),
      updatedAt: new Date()
    }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SettingsService]
    });
    service = TestBed.inject(SettingsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Ensure no outstanding requests
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  // Placeholder for more tests

  // Tests for getAllSettings
  describe('getAllSettings', () => {
    it('should retrieve all settings via GET request', () => {
      service.getAllSettings().subscribe(settings => {
        expect(settings.length).toBe(2);
        expect(settings).toEqual(mockSettings);
      });

      const req = httpMock.expectOne(baseUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockSettings);
    });

    it('should handle errors for getAllSettings', () => {
      service.getAllSettings().subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(500);
        }
      });

      const req = httpMock.expectOne(baseUrl);
      req.flush({ message: 'Error fetching settings' }, { status: 500, statusText: 'Server Error' });
    });
  });

  // Tests for getSettingById
  describe('getSettingById', () => {
    it('should retrieve a specific setting by ID via GET request', () => {
      const settingId = 1;
      service.getSettingById(settingId).subscribe(setting => {
        expect(setting).toEqual(mockSetting);
      });

      const req = httpMock.expectOne(`${baseUrl}/${settingId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockSetting);
    });

    it('should handle errors when setting not found (404) for getSettingById', () => {
      const settingId = 999; // Non-existent ID
      service.getSettingById(settingId).subscribe({
        next: () => fail('should have failed with 404 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(404);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${settingId}`);
      req.flush({ message: 'Setting not found' }, { status: 404, statusText: 'Not Found' });
    });
  });

  // Tests for addSetting
  describe('addSetting', () => {
    const newSettingPayload: Omit<Setting, 'id' | 'createdAt' | 'updatedAt'> = {
      name: 'NewSetting',
      settingValue: 'NewValue',
      category: 'Custom',
      settingValueType: 'string',
      defaultSettingValue: 'DefaultNew',
      createdBy: 1,
      updatedBy: 1
      // parentId and userId are optional
    };
    // The service method returns { message: string; setting: Setting }
    // So, the flushed response should include the full setting object with id, createdAt, updatedAt
    const createdSetting: Setting = {
      id: 3, // Assuming server assigns ID 3
      ...newSettingPayload,
      createdAt: new Date(),
      updatedAt: new Date()
    };
    const expectedResponse = { message: 'Setting created successfully', setting: createdSetting };

    it('should send new setting data via POST request', () => {
      service.addSetting(newSettingPayload).subscribe(response => {
        expect(response).toEqual(expectedResponse);
      });

      const req = httpMock.expectOne(baseUrl); // POST to baseUrl
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newSettingPayload);
      req.flush(expectedResponse);
    });

    it('should handle validation errors (400) for addSetting', () => {
      const invalidPayload: any = { ...newSettingPayload, name: '' }; // Example: empty name
      service.addSetting(invalidPayload).subscribe({
        next: () => fail('should have failed with a 400 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(400);
        }
      });

      const req = httpMock.expectOne(baseUrl);
      req.flush({ error: 'Validation failed', errors: { name: ['Name is required']}}, { status: 400, statusText: 'Bad Request' });
    });
  });

  // Tests for updateSetting
  describe('updateSetting', () => {
    const settingToUpdate: Setting = {
      ...mockSetting, // Use existing mock as base
      settingValue: 'Updated Portal Name',
      updatedAt: new Date() // Simulate an update timestamp
    };
    // The service method returns { message: string; setting: Setting }
    const expectedResponse = { message: 'Setting updated successfully', setting: settingToUpdate };

    it('should send updated setting data via PUT request', () => {
      service.updateSetting(settingToUpdate).subscribe(response => {
        expect(response).toEqual(expectedResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/${settingToUpdate.id}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(settingToUpdate);
      req.flush(expectedResponse);
    });

    it('should handle errors if setting to update is not found (404)', () => {
      const nonExistentSetting: Setting = { ...settingToUpdate, id: 999 };
      service.updateSetting(nonExistentSetting).subscribe({
        next: () => fail('should have failed with 404 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(404);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${nonExistentSetting.id}`);
      req.flush({ message: 'Setting not found for update' }, { status: 404, statusText: 'Not Found' });
    });
  });

  // Tests for deleteSetting
  describe('deleteSetting', () => {
    const settingIdToDelete = 1;
    const expectedDeleteResponse = { message: 'Setting deleted successfully' };

    it('should send a DELETE request with the setting ID', () => {
      service.deleteSetting(settingIdToDelete).subscribe(response => {
        expect(response).toEqual(expectedDeleteResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/${settingIdToDelete}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(expectedDeleteResponse);
    });

    it('should handle errors if setting to delete is not found (404)', () => {
      const nonExistentId = 999;
      service.deleteSetting(nonExistentId).subscribe({
        next: () => fail('should have failed with 404 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(404);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${nonExistentId}`);
      req.flush({ message: 'Setting not found for delete' }, { status: 404, statusText: 'Not Found' });
    });
  });

  // Tests for getSettingsByCategory
  describe('getSettingsByCategory', () => {
    const category = 'General';
    const generalSettings = mockSettings.filter(s => s.category === category);

    it('should retrieve settings for a specific category via GET request', () => {
      service.getSettingsByCategory(category).subscribe(settings => {
        expect(settings.length).toBe(generalSettings.length);
        expect(settings).toEqual(generalSettings);
      });

      const req = httpMock.expectOne(`${baseUrl}/Category/${encodeURIComponent(category)}`);
      expect(req.request.method).toBe('GET');
      req.flush(generalSettings);
    });

    it('should return an empty array if category has no settings', () => {
      const emptyCategory = 'NonExistentCategory';
      service.getSettingsByCategory(emptyCategory).subscribe(settings => {
        expect(settings.length).toBe(0);
        expect(settings).toEqual([]);
      });

      const req = httpMock.expectOne(`${baseUrl}/Category/${encodeURIComponent(emptyCategory)}`);
      req.flush([]);
    });
  });

  // Tests for searchSettings
  describe('searchSettings', () => {
    it('should search settings by name via GET request with query params', () => {
      const nameQuery = 'SiteName';
      const expectedResult = [mockSetting];
      service.searchSettings(nameQuery).subscribe(settings => {
        expect(settings).toEqual(expectedResult);
      });

      const req = httpMock.expectOne(`${baseUrl}/Search?name=${nameQuery}&limit=10&offset=0`);
      expect(req.request.method).toBe('GET');
      req.flush(expectedResult);
    });

    it('should search settings by category via GET request with query params', () => {
      const categoryQuery = 'General';
      const expectedResult = mockSettings.filter(s => s.category === categoryQuery);
      service.searchSettings(undefined, categoryQuery).subscribe(settings => {
        expect(settings).toEqual(expectedResult);
      });

      const req = httpMock.expectOne(`${baseUrl}/Search?category=${categoryQuery}&limit=10&offset=0`);
      expect(req.request.method).toBe('GET');
      req.flush(expectedResult);
    });

    it('should search settings by name and category with pagination via GET', () => {
      const nameQuery = 'Site';
      const categoryQuery = 'General';
      const limit = 5;
      const offset = 0;
      const expectedResult = [mockSetting]; // Assuming mockSetting matches 'Site' and 'General'

      service.searchSettings(nameQuery, categoryQuery, limit, offset).subscribe(settings => {
        expect(settings).toEqual(expectedResult);
      });

      const req = httpMock.expectOne(`${baseUrl}/Search?name=${nameQuery}&category=${categoryQuery}&limit=${limit}&offset=${offset}`);
      expect(req.request.method).toBe('GET');
      req.flush(expectedResult);
    });

    it('should return empty array if search yields no results', () => {
      service.searchSettings('NonExistentName').subscribe(settings => {
        expect(settings).toEqual([]);
      });

      const req = httpMock.expectOne(`${baseUrl}/Search?name=NonExistentName&limit=10&offset=0`);
      req.flush([]);
    });
  });

  // Tests for addOrUpdateSetting
  describe('addOrUpdateSetting', () => {
    // Payload for addOrUpdateSetting uses Omit<Setting, 'createdAt' | 'updatedAt'>
    // It might include an 'id' if it's an update, or not if it's a new add by name.
    const settingPayloadAdd: Omit<Setting, 'createdAt' | 'updatedAt'> = {
      id: 0, // Assuming ID 0 signifies a new record for AddOrUpdate
      name: 'NewOrUpdatedSetting',
      settingValue: 'SomeValue',
      category: 'Advanced',
      settingValueType: 'string',
      defaultSettingValue: 'DefaultVal',
      createdBy: 1,
      updatedBy: 1
    };
    const settingPayloadUpdate: Omit<Setting, 'createdAt' | 'updatedAt'> = {
      id: 1, // Existing ID for update scenario
      name: 'SiteName', // Existing name
      settingValue: 'Updated Gatekeeper Portal via AddOrUpdate',
      category: 'General',
      settingValueType: 'string',
      defaultSettingValue: 'Default Portal Value',
      createdBy: 1, // createdBy might not be updated by this call, depends on server logic
      updatedBy: 2 // Simulate updatedBy a different user
    };

    // This mock was for a generic response, specific tests below will define their own response mocks.
    // const returnedSetting: Setting = { ... };
    // const expectedResponse = { message: 'Setting processed successfully', setting: returnedSetting };

    it('should send setting data via POST for addOrUpdate (add scenario)', () => {
      // Server returns the created setting, including its new ID and timestamps
      const serverGeneratedId = 4;
      const addResponseMock: Setting = {
        ...settingPayloadAdd, // Contains id: 0 from payload
        id: serverGeneratedId, // Server assigns new ID
        createdAt: new Date(),
        updatedAt: new Date()
      };
      const specificExpectedResponse = { message: 'Setting added', setting: addResponseMock };

      service.addOrUpdateSetting(settingPayloadAdd).subscribe(response => {
        expect(response).toEqual(specificExpectedResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/AddOrUpdate`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(settingPayloadAdd);
      req.flush(specificExpectedResponse);
    });

    it('should send setting data via POST for addOrUpdate (update scenario)', () => {
      // Server returns the updated setting, potentially with new updatedAt
      const updatedTimestamp = new Date();
      const updateResponseMock: Setting = {
        ...settingPayloadUpdate, // Contains id: 1 from payload
        createdAt: mockSetting.createdAt, // Assuming createdAt doesn't change
        updatedAt: updatedTimestamp
      };
      const specificExpectedResponse = { message: 'Setting updated', setting: updateResponseMock };

      service.addOrUpdateSetting(settingPayloadUpdate).subscribe(response => {
        expect(response).toEqual(specificExpectedResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/AddOrUpdate`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(settingPayloadUpdate);
      req.flush(specificExpectedResponse);
    });

    it('should handle errors for addOrUpdateSetting', () => {
      const errorPayload: any = { ...settingPayloadAdd, name: null }; // Invalid payload
      service.addOrUpdateSetting(errorPayload).subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(400);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/AddOrUpdate`);
      req.flush({ message: 'Validation error' }, { status: 400, statusText: 'Bad Request' });
    });
  });
});
