import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Role } from '../models/role.model'; // Adjust the path to your Role model }
/**
 * Example interface representing your Role model from .NET.
 * Adjust property names as per your actual model.
 */


@Injectable({
  providedIn: 'root',
})
export class RoleService {
  // Your .NET Core Web API URL. Adjust to your environment or server endpoint.
  private baseUrl = '/api/Role';

  constructor(private http: HttpClient) { }

  /**
   * Retrieves all roles via GET /api/Role.
   */
  getAllRoles(): Observable<Role[]> {
    return this.http.get<Role[]>(this.baseUrl);
  }

  /**
   * Retrieves a single role by Id.
   * GET /api/Role/{id}
   * @param id The unique role Id.
   */
  getRoleById(id: number): Observable<Role> {
    const url = `${this.baseUrl}/${id}`;
    return this.http.get<Role>(url);
  }

  /**
   * Retrieves a single role by name.
   * GET /api/Role/by-name/{roleName}
   * @param roleName The name of the role.
   */
  getRoleByName(roleName: string): Observable<Role> {
    const url = `${this.baseUrl}/by-name/${encodeURIComponent(roleName)}`;
    return this.http.get<Role>(url);
  }

  /**
   * Creates a new role.
   * POST /api/Role
   * @param role A Role object to create on the server.
   */
  addRole(role: Omit<Role, 'id'>): Observable<{ message: string; role: Role }> {
    // For add, you might not have an id initially
    return this.http.post<{ message: string; role: Role }>(this.baseUrl, role);
  }

  /**
   * Updates an existing role.
   * PUT /api/Role/{id}
   * @param role The updated role object, including the existing role Id.
   */
  updateRole(role: Role): Observable<{ message: string; role: Role }> {
    const url = `${this.baseUrl}/${role.id}`;
    return this.http.put<{ message: string; role: Role }>(url, role);
  }
}
