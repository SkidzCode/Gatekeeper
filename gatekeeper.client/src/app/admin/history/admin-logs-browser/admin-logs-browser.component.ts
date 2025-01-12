import {
  Component,
  OnInit,
  AfterViewInit,
  ViewChild
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator, PageEvent } from '@angular/material/paginator';

export interface ChainedLogEntry {
  ['@t']?: string;
  ['@mt']?: string;
  RequestId?: string;
  ConnectionId?: string;
  CorrelationId?: string;
  UserId?: number;
  E_UserId?: number;
  ChainHash?: string;
  [key: string]: any;
}

@Component({
  selector: 'app-admin-logs-browser',
  templateUrl: './admin-logs-browser.component.html',
  styleUrls: ['./admin-logs-browser.component.scss'],
  standalone: false
})
export class AdminLogsBrowserComponent implements OnInit, AfterViewInit {

  /** Reactive form for filters */
  filtersForm!: FormGroup;

  /** If you want to store all logs in memory */
  logs: ChainedLogEntry[] = [];

  /** MatTableDataSource for table + pagination */
  dataSource = new MatTableDataSource<ChainedLogEntry>([]);

  displayedColumns: string[] = [
    'timestamp',
    'message',
    'requestId',
    'connectionId',
    'correlationId',
    'userId',
    'chainHash'
  ];

  detailColumns = [
    'timestamp',
    'requestId',
    'connectionId',
    'correlationId',
    'userId'
  ];

  messageRow = ['message'];

  loadingLogs = false;

  // The top/bottom paginators
  @ViewChild('paginatorTop') paginatorTop!: MatPaginator;
  @ViewChild('paginatorBottom') paginatorBottom!: MatPaginator;

  constructor(
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private http: HttpClient
  ) { }

  ngOnInit(): void {
    this.filtersForm = this.fb.group({
      logDate: [null, Validators.required],
      logTime: [''],
      requestId: [''],
      connectionId: [''],
      correlationId: [''],
      userId: ['']
    });
  }

  /**
   * Connect MatPaginator after view initialization
   */
  ngAfterViewInit() {
    // Use the TOP paginator as the "primary" for the dataSource
    this.dataSource.paginator = this.paginatorTop;

    // 1) When the top paginator changes, mirror that on the bottom
    this.paginatorTop.page.subscribe((event: PageEvent) => {
      this.paginatorBottom.pageIndex = event.pageIndex;
      this.paginatorBottom.pageSize = event.pageSize;
      // Because the data source is actually attached to the top paginator,
      // you do NOT need to reset the dataSource.paginator here—it’s already set.
    });

    // 2) When the bottom paginator changes, mirror that on the top
    // Bottom paginator event
    this.paginatorBottom.page.subscribe((event: PageEvent) => {
      // Just update top’s pageIndex/pageSize
      this.paginatorTop.pageIndex = event.pageIndex;
      this.paginatorTop.pageSize = event.pageSize;
      // Then, re-trigger the top paginator’s page event manually:
      this.paginatorTop._changePageSize(event.pageSize);
    });
  }

  onLoadLogs(): void {
    if (this.filtersForm.invalid) {
      this.showSnackBar('Please select a date before loading logs.');
      return;
    }
    const dateValue: Date = this.filtersForm.value.logDate;
    const timeValue: string = this.filtersForm.value.logTime || '';
    const dateTime = new Date(dateValue);
    if (timeValue) {
      const [hours, minutes] = timeValue.split(':');
      dateTime.setHours(+hours || 0, +minutes || 0, 0, 0);
    }

    const formattedDateTime = this.formatDateTimeForApi(dateTime);

    this.loadingLogs = true;
    this.logs = [];
    // Clear out the table
    this.dataSource.data = [];

    this.http
      .get<ChainedLogEntry[]>(`/api/logs/chained-logs?dateTime=${formattedDateTime}`)
      .subscribe({
        next: (data) => {
          this.logs = data || [];
          // Initially load all logs into the data source
          this.dataSource.data = this.logs;
          this.loadingLogs = false;
          // Optionally apply filters right away
          this.onApplyFilter();
        },
        error: (err) => {
          console.error('Error fetching logs:', err);
          this.showSnackBar('Error fetching logs. Check console for details.');
          this.loadingLogs = false;
        }
      });
  }

  /**
   * Applies client-side filters to the loaded logs, then updates dataSource.
   */
  onApplyFilter(): void {
    const { requestId, connectionId, correlationId, userId } = this.filtersForm.value;
    const filtered = this.logs.filter((log) => {
      // RequestId
      if (requestId && !this.containsIgnoreCase(log.RequestId, requestId)) {
        return false;
      }
      // ConnectionId
      if (connectionId && !this.containsIgnoreCase(log.ConnectionId, connectionId)) {
        return false;
      }
      // CorrelationId
      if (correlationId && !this.containsIgnoreCase(log.CorrelationId, correlationId)) {
        return false;
      }
      // UserId
      const combinedUserId = log.UserId ?? log.E_UserId;
      if (userId && userId !== '' && +userId > 0) {
        if (combinedUserId !== +userId) {
          return false;
        }
      }
      return true;
    });
    // Update the data in the table
    this.dataSource.data = filtered;
  }

  filterOutLogs(mtValue: string): void {
    this.dataSource.data = this.dataSource.data.filter(log => log['@mt'] !== mtValue);
  }

  getFormattedMessage(log: ChainedLogEntry): string {
    const originalMessage = log['@mt'] || '';
    if (!originalMessage) {
      return '';
    }
    let message = originalMessage;
    for (const key of Object.keys(log)) {
      const placeholder = `{${key}}`;
      if (message.includes(placeholder)) {
        const regex = new RegExp(`\\{${key}\\}`, 'g');
        message = message.replace(regex, String(log[key] ?? ''));
      }
    }
    return message;
  }

  private containsIgnoreCase(fieldValue: string | undefined, searchValue: string): boolean {
    if (!fieldValue) return false;
    return fieldValue.toLowerCase().includes(searchValue.toLowerCase());
  }

  private formatDateTimeForApi(date: Date): string {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    const seconds = date.getSeconds().toString().padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
  }

  private showSnackBar(message: string, action: string = 'OK', duration: number = 3000): void {
    this.snackBar.open(message, action, { duration });
  }
}
