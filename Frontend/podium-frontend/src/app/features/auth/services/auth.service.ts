import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, catchError, Observable, tap, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../../../environments/environment';
import {
  LoginRequest,
  RegisterRequest,
  LoginResponse,
  CurrentUser,
} from '../../../core/models/auth.models';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private apiUrl = `${environment.apiUrl}/Auth`;

  private currentUserSubject = new BehaviorSubject<CurrentUser | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor() {
    // Load user from localStorage on service initialization
    const storedUser = localStorage.getItem('currentUser');
    if (storedUser) {
      try {
        const user = JSON.parse(storedUser);
        this.currentUserSubject.next(user);
      } catch (error) {
        console.error('Error parsing stored user:', error);
        localStorage.removeItem('currentUser');
      }
    }
  }

  get currentUserValue(): CurrentUser | null {
    return this.currentUserSubject.value;
  }

  getRegistrationOptions(): Observable<{ bands: any[]; roles: string[] }> {
    return this.http.get<{ bands: any[]; roles: string[] }>(`${this.apiUrl}/registration-options`);
  }

  login(email: string, password: string): Observable<LoginResponse> {
    const request: LoginRequest = { email, password };

    return this.http
      .post<LoginResponse>(`${this.apiUrl}/login`, request)
      .pipe(tap((response) => this.setAuthData(response)));
  }

  register(request: RegisterRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/register`, request)
      .pipe(tap((response) => this.setAuthData(response)));
  }

  logout(): void {
    // Clear local storage
    localStorage.removeItem('currentUser');
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');

    // Clear current user
    this.currentUserSubject.next(null);

    // Navigate to login
    this.router.navigate(['/login']);
  }

refreshToken(): Observable<LoginResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      // If no token, we can't refresh. Log out immediately.
      this.logout();
      return throwError(() => new Error('No refresh token'));
    }

    return this.http
      .post<LoginResponse>(`${this.apiUrl}/refresh`, { refreshToken })
      .pipe(
        tap((response) => this.setAuthData(response)),
        catchError((err) => {
          // If refresh fails (e.g. refresh token itself is expired), strictly logout
          this.logout();
          return throwError(() => err);
        })
      );
  }

  isAuthenticated(): boolean {
    const user = this.currentUserValue;
    if (!user) return false;

    // Check if token is expired
    const tokenExpiration = new Date(user.tokenExpiration);
    return tokenExpiration > new Date();
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  hasRole(role: string): boolean {
    const user = this.currentUserValue;
    return user?.roles?.includes(role) || false;
  }

  hasAnyRole(roles: string[]): boolean {
    const user = this.currentUserValue;
    return user ? roles.some((role) => user.roles?.includes(role)) : false;
  }

  hasPermission(permission: string): boolean {
    const user = this.currentUserValue;
    return user?.permissions?.includes(permission) || false;
  }

  hasAnyPermission(permissions: string[]): boolean {
    const user = this.currentUserValue;
    return user ? permissions.some((perm) => user.permissions?.includes(perm)) : false;
  }

  hasAllPermissions(permissions: string[]): boolean {
    const user = this.currentUserValue;
    return user ? permissions.every((perm) => user.permissions?.includes(perm)) : false;
  }

  getCurrentUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${this.apiUrl}/me`);
  }

  updateProfile(data: any): Observable<CurrentUser> {
    return this.http.put<CurrentUser>(`${this.apiUrl}/profile`, data).pipe(
      tap((user) => {
        // Update current user with new profile data
        const currentUser = this.currentUserValue;
        if (currentUser) {
          const updatedUser = { ...currentUser, ...user };
          this.currentUserSubject.next(updatedUser);
          localStorage.setItem('currentUser', JSON.stringify(updatedUser));
        }
      })
    );
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/change-password`, {
      currentPassword,
      newPassword,
    });
  }

  requestPasswordReset(email: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(token: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reset-password`, {
      token,
      newPassword,
    });
  }

  // ============================================
  // PRIVATE HELPER METHOD - THIS WAS MISSING
  // ============================================
  private setAuthData(response: any): void {
    console.log('🔵 setAuthData called with:', response);

    // Backend returns 'accessToken' but we need 'token'
    const token = response.token || response.accessToken;
    const refreshToken = response.refreshToken;

    // Store tokens
    localStorage.setItem('token', token);
    localStorage.setItem('refreshToken', refreshToken);
    console.log('✅ Tokens stored');

    // Decode JWT to extract user info
    let decodedToken: any = {};
    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
          })
          .join('')
      );
      decodedToken = JSON.parse(jsonPayload);
      console.log('✅ Decoded JWT:', decodedToken);
    } catch (error) {
      console.error('❌ Error decoding JWT:', error);
    }

    // Extract user info from JWT claims
    const fullName = decodedToken.unique_name || '';
    const nameParts = fullName.split(' ');
    const firstName = nameParts[0] || '';
    const lastName = nameParts.slice(1).join(' ') || '';
    const role = decodedToken.role || '';
    const roles = Array.isArray(role) ? role : role ? [role] : [];

    // Create CurrentUser object
    const currentUser: CurrentUser = {
      userId: response.userId || decodedToken.nameid,
      email: response.email || decodedToken.email || decodedToken.sub,
      firstName: firstName,
      lastName: lastName,
      roles: roles, // Now populated from JWT!
      permissions: response.permissions || [],
      token: token,
      refreshToken: refreshToken,
      tokenExpiration: new Date(response.expiresAt || decodedToken.exp * 1000),
      studentId: response.studentId,
      guardianId: response.guardianId,
      bandStaffId: response.bandStaffId,
      directorId: response.directorId,
      bandId: response.bandId,
    };

    console.log('✅ Created currentUser:', currentUser);

    // Store user
    localStorage.setItem('currentUser', JSON.stringify(currentUser));

    // Update BehaviorSubject
    this.currentUserSubject.next(currentUser);
    console.log('✅ Auth state updated');
  }
}
