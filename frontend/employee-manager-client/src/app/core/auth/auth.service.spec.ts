import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { CurrentUser, LoginResult } from './auth.model';
import { AuthService, SESSION_STORAGE_KEY } from './auth.service';

const testUser: CurrentUser = {
  employeeId: 'e1',
  email: 'margaret.hayes@example.com',
  firstName: 'Margaret',
  lastName: 'Hayes',
  jobTitle: 'Chief Executive Officer',
  role: 'Manager',
};

function loginResult(expiresInMs = 60_000): LoginResult {
  return {
    accessToken: 'token-1',
    expiresAt: new Date(Date.now() + expiresInMs).toISOString(),
    user: testUser,
  };
}

describe('AuthService', () => {
  let http: HttpTestingController;

  function createService(): AuthService {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    return TestBed.inject(AuthService);
  }

  beforeEach(() => localStorage.clear());
  afterEach(() => http.verify());

  it('posts the credentials and stores the session', () => {
    const service = createService();
    let user: CurrentUser | undefined;
    service.login('margaret.hayes@example.com', 'secret').subscribe((u) => (user = u));

    const req = http.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'margaret.hayes@example.com', password: 'secret' });
    req.flush(loginResult());

    expect(user).toEqual(testUser);
    expect(service.currentUser()).toEqual(testUser);
    expect(service.accessToken()).toBe('token-1');
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toContain('token-1');
  });

  it('clears the session on logout', () => {
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(loginResult()));
    const service = createService();

    service.logout();

    expect(service.currentUser()).toBeNull();
    expect(service.accessToken()).toBeNull();
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('restores a stored session', () => {
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(loginResult()));

    const service = createService();

    expect(service.currentUser()).toEqual(testUser);
    expect(service.accessToken()).toBe('token-1');
  });

  it('ignores an expired stored session', () => {
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(loginResult(-1000)));

    const service = createService();

    expect(service.currentUser()).toBeNull();
    expect(service.accessToken()).toBeNull();
  });
});
