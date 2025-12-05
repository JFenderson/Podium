import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TokenService } from './token.service';
import {
  User,
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  RefreshTokenRequest
} from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private tokenService = inject(TokenService);
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();
  private readonly authUrl = `${environment.apiUrl}/auth`;

  constructor() {
    // Check if user is already authenticated
    if (this.tokenService.isAuthenticated()) {
      this.loadCurrentUser();
    }
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.authUrl}/register`, request).pipe(
      tap(response => this.handleAuthResponse(response))
    );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.authUrl}/login`, request).pipe(
      tap(response => this.handleAuthResponse(response))
    );
  }

  logout(): Observable<any> {
    const refreshToken = this.tokenService.getRefreshToken();
    return this.http.post(`${this.authUrl}/logout`, { refreshToken }).pipe(
      tap(() => {
        this.tokenService.clearTokens();
        this.currentUserSubject.next(null);
      })
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.tokenService.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const request: RefreshTokenRequest = { refreshToken };
    return this.http.post<AuthResponse>(`${this.authUrl}/refresh`, request).pipe(
      tap(response => {
        this.tokenService.setTokens(
          response.accessToken,
          response.refreshToken,
          new Date(response.expiresAt)
        );
      })
    );
  }

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.authUrl}/me`).pipe(
      tap(user => this.currentUserSubject.next(user))
    );
  }

  isAuthenticated(): boolean {
    return this.tokenService.isAuthenticated();
  }

  private handleAuthResponse(response: AuthResponse): void {
    this.tokenService.setTokens(
      response.accessToken,
      response.refreshToken,
      new Date(response.expiresAt)
    );
    this.loadCurrentUser();
  }

  private loadCurrentUser(): void {
    this.getCurrentUser().subscribe({
      error: () => {
        this.tokenService.clearTokens();
        this.currentUserSubject.next(null);
      }
    });
  }
}