import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { CurrentUser } from '../../core/auth/auth.model';
import { AuthService } from '../../core/auth/auth.service';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let login: ReturnType<typeof vi.fn>;
  let router: Router;

  async function render(response: () => Observable<CurrentUser>, returnUrl?: string) {
    login = vi.fn(response);
    TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), { provide: AuthService, useValue: { login } }],
    });
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    fixture = TestBed.createComponent(Login);
    fixture.componentRef.setInput('returnUrl', returnUrl);
    await fixture.whenStable();
  }

  async function submit(email: string, password: string): Promise<HTMLElement> {
    const element = fixture.nativeElement as HTMLElement;
    for (const [id, value] of [
      ['email', email],
      ['password', password],
    ]) {
      const input = element.querySelector<HTMLInputElement>(`#${id}`)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    return element;
  }

  it('signs in and goes to the return URL', async () => {
    await render(() => of({} as CurrentUser), '/org-chart?rootId=1');

    await submit('margaret.hayes@example.com', 'secret');

    expect(login).toHaveBeenCalledWith('margaret.hayes@example.com', 'secret');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/org-chart?rootId=1');
  });

  it('ignores a return URL that points to another site', async () => {
    await render(() => of({} as CurrentUser), '//evil.example.com');

    await submit('margaret.hayes@example.com', 'secret');

    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('shows an error for wrong credentials', async () => {
    await render(() => throwError(() => new HttpErrorResponse({ status: 401 })));

    const element = await submit('margaret.hayes@example.com', 'wrong');

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Incorrect email or password.',
    );
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('shows a generic error when the API is unreachable', async () => {
    await render(() => throwError(() => new HttpErrorResponse({ status: 0 })));

    const element = await submit('margaret.hayes@example.com', 'secret');

    expect(element.querySelector('[role="alert"]')?.textContent).toContain("Couldn't sign in.");
  });

  it('does not submit an invalid form', async () => {
    await render(() => of({} as CurrentUser));

    const element = await submit('not-an-email', '');

    expect(login).not.toHaveBeenCalled();
    expect(element.textContent).toContain('Enter a valid email address.');
    expect(element.textContent).toContain('Enter your password.');
  });
});
