import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { Router } from '@angular/router';
import { UserService } from '../../../core/services/user/user.service';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { User } from '../../../shared/models/user.model';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss'],
  standalone: false,
})
export class UserListComponent implements OnInit, AfterViewInit {
  displayedColumns: string[] = ['pic', 'id', 'username', 'email', 'actions'];
  dataSource = new MatTableDataSource<User>([]);

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  constructor(
    private userService: UserService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadUsers();
  }

  ngAfterViewInit(): void {
    // Setting up the sort and paginator after the view is initialized
    // (and after data is loaded, see loadUsers() below).
  }

  loadUsers(): void {
    this.userService.getUsers().subscribe({
      next: (data) => {
        this.dataSource.data = data;
        // Now that data is available, set the sort and paginator references
        // If this is done in ngAfterViewInit only, it might be too early 
        // if data is loaded asynchronously.
        this.dataSource.sort = this.sort;
        this.dataSource.paginator = this.paginator;
      },
      error: (err) => console.error(err),
    });
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    // Trim and lowercase the filter value
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  editUser(user: User): void {
    // Navigate to the user-edit route: /admin/users/edit/:id
    this.router.navigate(['/admin', 'users', 'edit', user.id]);
  }

  onImageError(event: Event): void {
    const element = event.target as HTMLImageElement;
    element.style.display = 'none'; // Hide the image
  }
}
