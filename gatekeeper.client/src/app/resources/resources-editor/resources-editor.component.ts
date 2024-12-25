import { Component, OnInit } from '@angular/core';
import { ResourcesService, ResourceEntry, AddResourceEntryRequest, UpdateResourceEntryRequest } from '../../services/ResourcesService';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatTableDataSource } from '@angular/material/table';
import { ResourceLoaderService } from '../../services/resource-loader-service.service';
@Component({
  selector: 'app-resources-editor',
  templateUrl: './resources-editor.component.html',
  styleUrls: ['./resources-editor.component.scss'],
  standalone: false,
})
export class ResourcesEditorComponent implements OnInit {
  resources: { [key: string]: string } = {};
  resourceKeys: string[] = [];

  resourceFiles: string[] = [
    'DialogLogin',
    'DialogPassword',
    'DialogRegister',
    'DialogVerify'
    // Add other resource file names here if desired
  ];

  selectedFile: string = 'Dialog';
  dataSource = new MatTableDataSource<ResourceEntry>([]);
  displayedColumns: string[] = ['key', 'value','comment', 'actions'];

  // For editing inline
  editingKey: string | null = null;
  editForm = new FormGroup({
    value: new FormControl('', Validators.required),
    comment: new FormControl('', Validators.required)
  });

  // For adding a new entry
  addForm = new FormGroup({
    key: new FormControl('', Validators.required),
    value: new FormControl('', Validators.required),
    comment: new FormControl('', Validators.required)
  });

  loading: boolean = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  constructor(private resourcesService: ResourcesService, private resourceLoader: ResourceLoaderService) { }

  ngOnInit() {
    this.resourceLoader.loadResourceFile('Dialog').subscribe(resourceDict => {
      this.resources = resourceDict;
      this.resourceKeys = Object.keys(resourceDict);
    });

    this.loadEntries(this.selectedFile);
  }

  loadEntries(fileName: string) {
    this.loading = true;
    this.errorMessage = null;
    this.successMessage = null;

    this.resourcesService.getEntries(fileName).subscribe({
      next: (entries) => {
        this.dataSource.data = entries;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = err;
        this.loading = false;
      }
    });
  }

  onFileChange() {
    this.loadEntries(this.selectedFile);
  }

  startEditing(entry: ResourceEntry) {
    this.editingKey = entry.key;
    this.editForm.patchValue({ value: entry.value, comment: entry.comment });
  }

  cancelEditing() {
    this.editingKey = null;
    this.editForm.reset();
  }

  saveEditing() {
    if (!this.editingKey) return;
    if (this.editForm.invalid) return;

    const updatedValue = this.editForm.get('value')?.value || '';
    const updatedComment = this.editForm.get('comment')?.value || ''; // Use `get()` to access the control
    const request: UpdateResourceEntryRequest = { type: 'string', value: updatedValue, comment: updatedComment };

    this.resourcesService.updateEntry(this.selectedFile, this.editingKey, request).subscribe({
      next: () => {
        const index = this.dataSource.data.findIndex(e => e.key === this.editingKey);
        if (index > -1) {
          this.dataSource.data[index].value = updatedValue;
          this.dataSource._updateChangeSubscription();
        }
        this.successMessage = 'Entry updated successfully.';
        this.cancelEditing();
      },
      error: (err) => {
        this.errorMessage = err;
      }
    });
  }


  addEntry() {
    if (this.addForm.invalid) return;

    const newKey = this.addForm.value.key || '';
    const newValue = this.addForm.value.value || '';
    const request: AddResourceEntryRequest = { key: newKey, value: newValue };

    this.resourcesService.addEntry(this.selectedFile, request).subscribe({
      next: (newEntry) => {
        this.dataSource.data = [...this.dataSource.data, newEntry];
        this.addForm.reset();
        this.successMessage = 'New entry added successfully.';
      },
      error: (err) => {
        this.errorMessage = err;
      }
    });
  }
}
