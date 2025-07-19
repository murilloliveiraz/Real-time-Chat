import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthenticationService } from '../services/authentication.service';

export const authGuard: CanActivateFn = (route, state): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree =>  {
  const currentUser = inject(AuthenticationService).currentUserValue;
  if (currentUser) {
    return true;
  }

  inject(Router).navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};
