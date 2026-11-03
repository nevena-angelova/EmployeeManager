import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

/** Lets only Managers through: signed-out users go to the login page, everyone else to the Hours page. */
export const managerGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.accessToken()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }
  return auth.currentUser()?.role === 'Manager' ? true : router.createUrlTree(['/allocations']);
};
