import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class DisabledGuardService implements CanActivate {
  constructor(private router: Router) { }

  canActivate(): boolean {
    if (!environment.mainSiteEnabled) {
      this.router.navigate(['/disabled']);
      return false;
    }
    return true;
  }
}
