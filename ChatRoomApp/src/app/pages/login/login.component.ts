import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  standalone: false,
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  user = {
    username: '',
    password: ''
  };
  errorMessage: string = '';

  constructor(private authService: AuthService, private router: Router) {}

  login() {
    console.log('🔄 Sending login request:', this.user);

    this.authService.login(this.user).subscribe({
      next: (response) => {
        console.log('✅ Login successful:', response);

        if (response.token) {
          localStorage.setItem('token', response.token); // Store token in localStorage
          alert('🎉 Login successful!');
          this.router.navigate(['/chat']); // Redirect to chat page
        } else {
          this.errorMessage = 'Login failed. No token received.';
        }
      },
      error: (error) => {
        console.error('❌ Login failed:', error);

        this.errorMessage = '';

        if (error.error?.errors) {
          this.errorMessage = Object.entries(error.error.errors)
            .map(([field, messages]) => `${field}: ${Array.isArray(messages) ? messages.join(', ') : messages}`)
            .join('\n');
          alert(`⚠️ Login Errors:\n${this.errorMessage}`);
        } else {
          this.errorMessage = error.error?.message || 'Invalid email or password.';
        }
      }
    });
  }
}
