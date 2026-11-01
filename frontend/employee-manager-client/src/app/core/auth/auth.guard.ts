import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

/** Sends signed-out users to the login page, remembering where they were going. */
export const authGuard: CanActivateFn = (_route, state) => {
  if (inject(AuthService).accessToken()) {
    return true;
  }
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
