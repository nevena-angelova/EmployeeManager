import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { App } from './app';
import { CurrentUser } from './core/auth/auth.model';
import { AuthService } from './core/auth/auth.service';

const ethan: CurrentUser = {
  employeeId: 'e1',
  email: 'ethan.clarke@example.com',
  firstName: 'Ethan',
  lastName: 'Clarke',
  jobTitle: 'Backend Developer',
  role: 'Employee',
};

describe('App', () => {
  const currentUser = signal<CurrentUser | null>(null);
  const logout = vi.fn(() => currentUser.set(null));

  async function render(user: CurrentUser | null): Promise<HTMLElement> {
    currentUser.set(user);
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), { provide: AuthService, useValue: { currentUser, logout } }],
    });
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should render title', async () => {
    const compiled = await render(null);
    expect(compiled.querySelector('h1')?.textContent).toContain('EmployeeManager');
  });

  it('hides the account area when signed out', async () => {
    const compiled = await render(null);
    expect(compiled.querySelector('.account')).toBeNull();
  });

  it('shows the signed-in user and their role', async () => {
    const compiled = await render(ethan);
    expect(compiled.querySelector('.user-name')?.textContent).toBe('Ethan Clarke');
    expect(compiled.querySelector('.role')?.textContent).toBe('Employee');
  });

  it('shows the Org chart link but not Employees to non-admins', async () => {
    const compiled = await render(ethan);
    const links = [...compiled.querySelectorAll('.main-nav a')].map((a) => a.textContent?.trim());
    expect(links).toEqual(['Org chart']);
  });

  it('shows the Employees link to Admins', async () => {
    const compiled = await render({ ...ethan, role: 'Admin' });
    const links = [...compiled.querySelectorAll('.main-nav a')].map((a) => a.textContent?.trim());
    expect(links).toEqual(['Org chart', 'Employees']);
  });

  it('signs out and goes to the login page', async () => {
    const compiled = await render(ethan);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    compiled.querySelector<HTMLButtonElement>('.account button')!.click();

    expect(logout).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });
});
