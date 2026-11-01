import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';

import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  function runGuard(token: string | null): boolean | UrlTree {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { accessToken: () => token } },
      ],
    });
    return TestBed.runInInjectionContext(
      () =>
        authGuard({} as ActivatedRouteSnapshot, { url: '/org-chart' } as RouterStateSnapshot) as
          boolean | UrlTree,
    );
  }

  it('lets signed-in users through', () => {
    expect(runGuard('token')).toBe(true);
  });

  it('redirects signed-out users to the login page with a return URL', () => {
    const result = runGuard(null);

    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe(
      '/login?returnUrl=%2Forg-chart',
    );
  });
});
