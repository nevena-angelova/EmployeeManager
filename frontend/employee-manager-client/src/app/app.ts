import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from './core/auth/auth.service';

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly title = signal('EmployeeManager');
  protected readonly user = this.auth.currentUser;
  protected readonly isAdmin = computed(() => this.user()?.role === 'Admin');

  protected signOut(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
