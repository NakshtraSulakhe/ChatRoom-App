import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  standalone: false,
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  user = {
    username: '',
    email: '',
    password: ''
  };
  errorMessage: string = '';

  constructor(private authService: AuthService, private router: Router) {}
  
  goToRegister() {
    this.router.navigate(['/login']);
  }

  register() {
    this.authService.register(this.user).subscribe({
      next: (response) => {
        console.log('✅ User registered successfully:', response);
        alert('🎉 Registration successful!');
        this.router.navigate(['/login']); // Redirect to login page
      },
      error: (error) => {
        console.error('❌ Registration failed:', error);
  
        this.errorMessage = '';
  
        if (error.error?.errors) {
          // Convert validation errors into readable format
          this.errorMessage = Object.entries(error.error.errors)
            .map(([field, messages]) => {
              // Ensure messages is an array; if not, convert to string
              return `${field}: ${Array.isArray(messages) ? messages.join(', ') : messages}`;
            })
            .join('\n');
  
          alert(`⚠️ Validation Errors:\n${this.errorMessage}`);
        } else {
          this.errorMessage = '❌ Registration failed. Please check your input and try again.';
        }
      }
    });
  }
} 