import { Component, OnInit, AfterViewInit, viewChild } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { NotificationService } from '../../../core/services/site/notification.service';
import { Notification } from '../../../../../src/app/shared/models/notification.model';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

@Component({
  selector: 'app-notification',
  templateUrl: './notification.component.html',
  styleUrls: ['./notification.component.scss'],
  standalone: false,
})
export class NotificationComponent implements OnInit, AfterViewInit {
  dataSource = new MatTableDataSource<Notification>([]);
  readonly paginatorTop = viewChild.required<MatPaginator>('paginatorTop');
  readonly paginatorBottom = viewChild.required<MatPaginator>('paginatorBottom');

  loadingNotifications = false;
  selectedTabIndex = 0;

  constructor(
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private notificationService: NotificationService
  ) { }

  ngOnInit(): void {
    this.fetchNotificationLog();
  }

  ngAfterViewInit() {
    this.dataSource.paginator = this.paginatorTop();
    this.paginatorTop().page.subscribe((event: PageEvent) => {
      const paginatorBottom = this.paginatorBottom();
      paginatorBottom.pageIndex = event.pageIndex;
      paginatorBottom.pageSize = event.pageSize;
    });
    this.paginatorBottom().page.subscribe((event: PageEvent) => {
      const paginatorTop = this.paginatorTop();
      paginatorTop.pageIndex = event.pageIndex;
      paginatorTop.pageSize = event.pageSize;
      paginatorTop._changePageSize(event.pageSize);
    });
  }

  fetchNotificationLog(): void {
    this.loadingNotifications = true;
    this.notificationService.getAllNotifications().subscribe({
      next: (notifications) => {
        this.dataSource.data = notifications;
        this.loadingNotifications = false;
      },
      error: (err) => {
        console.error('Error fetching notifications:', err);
        this.showSnackBar('Error fetching notifications');
        this.loadingNotifications = false;
      }
    });
  }

  onTabChange(event: any): void {
    this.selectedTabIndex = event.index;
    if (this.selectedTabIndex === 1 && this.dataSource.data.length === 0) {
      this.fetchNotificationLog();
    }
  }

  private showSnackBar(message: string, action = 'OK', duration = 3000): void {
    this.snackBar.open(message, action, { duration });
  }
}
