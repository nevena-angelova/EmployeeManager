import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: { accessToken: () => string | null; logout: ReturnType<typeof vi.fn> };
  let router: Router;

  function setUp(token: string | null): void {
    auth = { accessToken: () => token, logout: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  }

  afterEach(() => backend.verify());

  it('adds the bearer token to API requests', () => {
    setUp('token-1');
    http.get(`${environment.apiUrl}/employees/org-chart`).subscribe();

    const req = backend.expectOne(`${environment.apiUrl}/employees/org-chart`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer token-1');
    req.flush([]);
  });

  it('leaves requests to other hosts alone', () => {
    setUp('token-1');
    http.get('https://example.com/data').subscribe();

    const req = backend.expectOne('https://example.com/data');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('signs out and goes to the login page when the API rejects the token', () => {
    setUp('token-1');
    http.get(`${environment.apiUrl}/employees/org-chart`).subscribe({ error: () => {} });

    backend
      .expectOne(`${environment.apiUrl}/employees/org-chart`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });

  it('does not redirect a failed sign-in', () => {
    setUp(null);
    http.post(`${environment.apiUrl}/auth/login`, {}).subscribe({ error: () => {} });

    backend
      .expectOne(`${environment.apiUrl}/auth/login`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logout).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
