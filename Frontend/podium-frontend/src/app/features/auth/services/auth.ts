import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { tap, catchError, map } from 'rxjs/operators';
import { Router } from '@angular/router';
import { environment } from '../../../../../environments/environment';
import {
  LoginDto,
  RegisterDto,
  LoginResponse,
  CurrentUser,
  RefreshTokenRequest,
  RegistrationOptions
} from '../../../core/models/auth';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = `${environment.apiUrl}/Auth`;
  
  private currentUserSubject = new BehaviorSubject<CurrentUser | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();
  
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasValidToken());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  setAuthData: any;

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // Check token validity on service initialization
    this.checkTokenValidity();
  }

  /**
   * Get current user value
   */
  get currentUserValue(): CurrentUser | null {
    return this.currentUserSubject.value;
  }

  /**
   * Check if user is authenticated
   */
  get isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  /**
   * Register a new user
   */
  register(dto: RegisterDto): Observable<any> {
    return this.http.post(`${this.API_URL}/register`, dto).pipe(
      tap(response => {
        if (environment.enableDebugLogging) {
          console.log('Registration successful:', response);
        }
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Get registration options (bands, roles)
   */
  getRegistrationOptions(): Observable<RegistrationOptions> {
    return this.http.get<RegistrationOptions>(`${this.API_URL}/registration-options`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Confirm email
   */
  confirmEmail(userId: string, token: string): Observable<any> {
    return this.http.get(`${this.API_URL}/confirm-email`, {
      params: { userId, token }
    }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Login user
   */
  login(dto: LoginDto): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.API_URL}/login`, dto).pipe(
      tap(response => this.setAuthData(response)),
      catchError(this.handleError)
    );
  }

  /**
   * Logout user
   */
  logout(): Observable<any> {
    const refreshToken = this.getRefreshToken();
    
    if (!refreshToken) {
      this.clearSession();
      return new Observable(observer => observer.complete());
    }

    return this.http.post(`${this.API_URL}/logout`, { refreshToken }).pipe(
      tap(() => this.clearSession()),
      catchError(error => {
        this.clearSession();
        return throwError(() => error);
      })
    );
  }

  /**
   * Refresh access token
   */
  refreshToken(): Observable<LoginResponse> {
    const refreshToken = this.getRefreshToken();
    
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    const request: RefreshTokenRequest = { refreshToken };
    
    return this.http.post<LoginResponse>(`${this.API_URL}/refresh`, request).pipe(
      tap(response => this.setAuthData(response)),
      catchError(error => {
        this.clearSession();
        return throwError(() => error);
      })
    );
  }

  /**
   * Get current user from backend
   */
getCurrentUser(): Observable<CurrentUser> {
  return this.http.get<CurrentUser>(`${this.API_URL}/me`);
}

  /**
   * Load current user data
   */
  private loadCurrentUser(): void {
    if (this.hasValidToken()) {
      this.getCurrentUser().subscribe({
        error: (error) => {
          console.error('Failed to load current user:', error);
          this.clearSession();
        }
      });
    }
  }

  /**
   * Set session data after login/refresh
   */
  private setSession(response: LoginResponse): void {
    localStorage.setItem(environment.tokenKey, response.accessToken);
    localStorage.setItem(environment.refreshTokenKey, response.refreshToken);
    localStorage.setItem(environment.tokenExpiry, response.expiresAt.toString());
    this.isAuthenticatedSubject.next(true);
  }

  /**
   * Clear session data
   */
  private clearSession(): void {
    localStorage.removeItem(environment.tokenKey);
    localStorage.removeItem(environment.refreshTokenKey);
    localStorage.removeItem(environment.userKey);
    localStorage.removeItem(environment.tokenExpiry);
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/auth/login']);
  }

  /**
   * Get access token
   */
  getToken(): string | null {
    return localStorage.getItem(environment.tokenKey);
  }

  /**
   * Get refresh token
   */
  private getRefreshToken(): string | null {
    return localStorage.getItem(environment.refreshTokenKey);
  }

  /**
   * Check if token is valid
   */
  private hasValidToken(): boolean {
    const token = this.getToken();
    const expiry = localStorage.getItem(environment.tokenExpiry);
    
    if (!token || !expiry) {
      return false;
    }

    const expiryDate = new Date(expiry);
    const now = new Date();
    
    return expiryDate > now;
  }

  /**
   * Check token validity and refresh if needed
   */
  private checkTokenValidity(): void {
    if (this.hasValidToken()) {
      this.isAuthenticatedSubject.next(true);
      this.loadCurrentUser();
    } else if (this.getRefreshToken()) {
      // Try to refresh token
      this.refreshToken().subscribe({
        next: () => this.loadCurrentUser(),
        error: () => this.clearSession()
      });
    } else {
      this.clearSession();
    }
  }

  /**
   * Store user in local storage
   */
  private setUserInStorage(user: CurrentUser): void {
    localStorage.setItem(environment.userKey, JSON.stringify(user));
  }

  /**
   * Get user from local storage
   */
  private getUserFromStorage(): CurrentUser | null {
    const userJson = localStorage.getItem(environment.userKey);
    return userJson ? JSON.parse(userJson) : null;
  }

  /**
   * Check if user has specific role
   */
hasRole(role: string): boolean {
  const user = this.currentUserValue;
  return user?.roles.includes(role) || false;
}

  /**
   * Check if user has any of the specified roles
   */
  hasAnyRole(roles: string[]): boolean {
    const user = this.currentUserValue;
    return user ? roles.some(role => user.roles.includes(role)) : false;
  }

  /**
   * Check if user has specific permission
   */
  hasPermission(permission: string): boolean {
    const user = this.currentUserValue;
    return user?.permissions?.includes(permission) ?? false;
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: any): Observable<never> {
    let errorMessage = 'An error occurred';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = error.error.message;
    } else {
      // Server-side error
      errorMessage = error.error?.message || error.message || error.statusText;
    }

    if (environment.enableDebugLogging) {
      console.error('Auth Service Error:', error);
    }

    return throwError(() => new Error(errorMessage));
  }
}