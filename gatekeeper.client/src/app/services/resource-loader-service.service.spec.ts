import { TestBed } from '@angular/core/testing';

import { ResourceLoaderServiceService } from './resource-loader-service.service';

describe('ResourceLoaderServiceService', () => {
  let service: ResourceLoaderServiceService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ResourceLoaderServiceService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
