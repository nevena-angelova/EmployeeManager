import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';

import { adminGuard } from './admin.guard';
import { Role } from './auth.model';
import { AuthService } from './auth.service';

describe('adminGuard', () => {
  function runGuard(token: string | null, role?: Role): boolean | UrlTree {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { accessToken: () => token, currentUser: () => (role ? { role } : null) },
        },
      ],
    });
    return TestBed.runInInjectionContext(
      () =>
        adminGuard({} as ActivatedRouteSnapshot, { url: '/employees' } as RouterStateSnapshot) as
          boolean | UrlTree,
    );
  }

  function serialize(result: boolean | UrlTree): string {
    return TestBed.inject(Router).serializeUrl(result as UrlTree);
  }

  it('lets Admins through', () => {
    expect(runGuard('token', 'Admin')).toBe(true);
  });

  it('sends other signed-in users to the org chart', () => {
    expect(serialize(runGuard('token', 'Manager'))).toBe('/org-chart');
  });

  it('redirects signed-out users to the login page with a return URL', () => {
    expect(serialize(runGuard(null))).toBe('/login?returnUrl=%2Femployees');
  });
});
