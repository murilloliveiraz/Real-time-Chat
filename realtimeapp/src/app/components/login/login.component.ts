import { Component } from '@angular/core';
import { AuthenticationService } from '../../services/authentication.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  username = '';
  password = '';
  errorMessage: string | null = null;

  constructor(private authService: AuthenticationService, private router: Router) { }

  ngOnInit(): void {
    if (this.authService.currentUserValue) {
      this.router.navigate(['/chat']);
    }
  }

  login(): void {
    this.errorMessage = null;
    this.authService.login(this.username, this.password).subscribe({
      next: () => {
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Login failed.';
        console.error('Login error:', err);
      }
    });
  }
}
