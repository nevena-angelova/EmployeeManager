import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';

import { Role } from './auth.model';
import { AuthService } from './auth.service';
import { managerGuard } from './manager.guard';

describe('managerGuard', () => {
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
        managerGuard(
          {} as ActivatedRouteSnapshot,
          { url: '/allocations/statistics' } as RouterStateSnapshot,
        ) as boolean | UrlTree,
    );
  }

  function serialize(result: boolean | UrlTree): string {
    return TestBed.inject(Router).serializeUrl(result as UrlTree);
  }

  it('lets Managers through', () => {
    expect(runGuard('token', 'Manager')).toBe(true);
  });

  it('sends other signed-in users to the Hours page', () => {
    expect(serialize(runGuard('token', 'Employee'))).toBe('/allocations');
  });

  it('sends Admins to the Hours page too', () => {
    expect(serialize(runGuard('token', 'Admin'))).toBe('/allocations');
  });

  it('redirects signed-out users to the login page with a return URL', () => {
    expect(serialize(runGuard(null))).toBe('/login?returnUrl=%2Fallocations%2Fstatistics');
  });
});
