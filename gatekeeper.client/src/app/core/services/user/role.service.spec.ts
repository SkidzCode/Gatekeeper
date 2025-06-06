import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';

import { RoleService } from './role.service';
import { Role } from '../../../shared/models/role.model';

describe('RoleService', () => {
  let service: RoleService;
  let httpMock: HttpTestingController;
  const baseUrl = '/api/Role';

  const mockRoles: Role[] = [
    { id: 1, roleName: 'Admin' },
    { id: 2, roleName: 'User' },
  ];

  const mockRole: Role = mockRoles[0];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [RoleService],
    });
    service = TestBed.inject(RoleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Make sure that there are no outstanding requests.
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAllRoles', () => {
    it('should return all roles on success', () => {
      service.getAllRoles().subscribe((roles) => {
        expect(roles.length).toBe(mockRoles.length);
        expect(roles).toEqual(mockRoles);
      });

      const req = httpMock.expectOne(baseUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockRoles);
    });

    it('should handle error when fetching all roles', () => {
      const errorMessage = 'Failed to fetch roles';
      service.getAllRoles().subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(500);
          expect(error.statusText).toBe('Server Error');
        },
      });

      const req = httpMock.expectOne(baseUrl);
      expect(req.request.method).toBe('GET');
      req.flush({ message: errorMessage }, { status: 500, statusText: 'Server Error' });
    });
  });

  describe('getRoleById', () => {
    it('should return a role by id on success', () => {
      const roleId = mockRole.id;
      service.getRoleById(roleId).subscribe(role => {
        expect(role).toEqual(mockRole);
      });

      const req = httpMock.expectOne(`${baseUrl}/${roleId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockRole);
    });

    it('should handle error when fetching a role by id', () => {
      const roleId = 999; // Non-existent ID
      service.getRoleById(roleId).subscribe({
        next: () => fail('should have failed with 404 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(404);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${roleId}`);
      expect(req.request.method).toBe('GET');
      req.flush({ message: 'Not Found' }, { status: 404, statusText: 'Not Found' });
    });
  });

  describe('getRoleByName', () => {
    it('should return a role by name on success', () => {
      const roleName = mockRole.roleName; // Corrected property
      const encodedRoleName = encodeURIComponent(roleName);
      service.getRoleByName(roleName).subscribe(role => {
        expect(role).toEqual(mockRole);
      });

      const req = httpMock.expectOne(`${baseUrl}/by-name/${encodedRoleName}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockRole);
    });

    it('should handle error when fetching a role by name', () => {
      const roleName = 'NonExistentRole';
      const encodedRoleName = encodeURIComponent(roleName);
      service.getRoleByName(roleName).subscribe({
        next: () => fail('should have failed with 404 error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(404);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/by-name/${encodedRoleName}`);
      expect(req.request.method).toBe('GET');
      req.flush({ message: 'Not Found' }, { status: 404, statusText: 'Not Found' });
    });
  });

  describe('addRole', () => {
    it('should add a new role and return it on success', () => {
      const newRoleData: Omit<Role, 'id'> = { roleName: 'New Role' }; // Corrected property
      const expectedResponse = { message: 'Role created', role: { id: 3, ...newRoleData } };

      service.addRole(newRoleData).subscribe(response => {
        expect(response).toEqual(expectedResponse);
      });

      const req = httpMock.expectOne(baseUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newRoleData);
      req.flush(expectedResponse);
    });

    it('should handle error when adding a role', () => {
      const newRoleData: Omit<Role, 'id'> = { roleName: 'New Role' }; // Corrected property
      service.addRole(newRoleData).subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(400); // Example: Bad request if role name exists
        }
      });

      const req = httpMock.expectOne(baseUrl);
      expect(req.request.method).toBe('POST');
      req.flush({ message: 'Role already exists' }, { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('updateRole', () => {
    it('should update an existing role and return it on success', () => {
      const updatedRole: Role = { ...mockRole, roleName: 'Administrator - Updated' }; // Corrected property
      const expectedResponse = { message: 'Role updated', role: updatedRole };

      service.updateRole(updatedRole).subscribe(response => {
        expect(response).toEqual(expectedResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/${updatedRole.id}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updatedRole);
      req.flush(expectedResponse);
    });

    it('should handle error when updating a role', () => {
      const roleToUpdate: Role = { id: 999, roleName: 'Ghost Role' }; // Corrected property, Non-existent ID
       service.updateRole(roleToUpdate).subscribe({
        next: () => fail('should have failed with an error'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(404);
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${roleToUpdate.id}`);
      expect(req.request.method).toBe('PUT');
      req.flush({ message: 'Role not found' }, { status: 404, statusText: 'Not Found' });
    });
  });
});
