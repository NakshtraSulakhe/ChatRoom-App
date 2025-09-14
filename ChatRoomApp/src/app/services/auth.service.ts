import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ReplaySubject, Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'https://localhost:44369/api/Auth'; // Backend API
  private userSubject = new ReplaySubject<any>(1); // Stores last emitted value
  private currentUser: any = null; // Store the latest user data


  constructor(private http: HttpClient) {
    this.loadUser();
  }

  register(user: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, user)
      .pipe(catchError(this.handleError));
  }

  login(credentials: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, credentials).pipe(
      map((response: any) => {
        if (response?.token) {
          this.storeToken(response.token);
          const user = this.decodeToken(response.token);
          this.userSubject.next(user);
        }
        return response;
      }),
      catchError(this.handleError)
    );
  }

  private decodeToken(token: string): any {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return {
        username: payload.unique_name,
        email: payload.email,
        exp: payload.exp
      };
    } catch (error) {
      console.error('Failed to decode token:', error);
      return null;
    }
  }
  getUserName(): Observable<string> {
    return this.userSubject.asObservable().pipe(
      map(user => user?.username || '')
    );
  }

  private loadUser() {
    const token = this.getToken();
    if (token && !this.isTokenExpired(token)) {
      const user = this.decodeToken(token);
      this.userSubject.next(user); // Emit user data
    } else {
      this.logout();
    }
  }

  logout() {
    // Step 1: Call the backend to remove user from online list
    this.http.post(`${this.apiUrl}/logout`, {}).subscribe({
      next: () => {
        // Step 2: Clear local storage and subject
        localStorage.removeItem('userToken');
        this.userSubject.next(null);
      },
      error: err => {
        console.error('Logout failed:', err);
        // Still clear token on client even if backend fails
        localStorage.removeItem('userToken');
        this.userSubject.next(null);
      }
    });
  }
  

  isAuthenticated(): boolean {
    const token = this.getToken();
    return token ? !this.isTokenExpired(token) : false;
  }

  private storeToken(token: string) {
    localStorage.setItem('userToken', token);
  }

  private getToken(): string | null {
    return localStorage.getItem('userToken');
  }

  private isTokenExpired(token: string): boolean {
    const decoded = this.decodeToken(token);
    if (!decoded?.exp) return true;
    return Date.now() >= decoded.exp * 1000;
  }

  private handleError(error: any) {
    console.error('API Error:', error);
    return throwError(() => new Error('Something went wrong!'));
  }
}
