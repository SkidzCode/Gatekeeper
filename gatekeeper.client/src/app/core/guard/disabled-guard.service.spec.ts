import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { DisabledGuardService } from './disabled-guard.service';
import { environment } from '../../../environments/environment';

describe('DisabledGuardService', () => {
  let guard: DisabledGuardService;
  let routerMock: any;

  beforeEach(() => {
    routerMock = {
      navigate: jasmine.createSpy('navigate')
    };

    TestBed.configureTestingModule({
      providers: [
        DisabledGuardService,
        { provide: Router, useValue: routerMock }
      ]
    });
    guard = TestBed.inject(DisabledGuardService);
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });

  describe('canActivate', () => {
    it('should return false and navigate to /disabled if mainSiteEnabled is false', () => {
      environment.mainSiteEnabled = false;
      const result = guard.canActivate();
      expect(result).toBe(false);
      expect(routerMock.navigate).toHaveBeenCalledWith(['/disabled']);
    });

    it('should return true if mainSiteEnabled is true', () => {
      environment.mainSiteEnabled = true;
      const result = guard.canActivate();
      expect(result).toBe(true);
      expect(routerMock.navigate).not.toHaveBeenCalled();
    });
  });
});
