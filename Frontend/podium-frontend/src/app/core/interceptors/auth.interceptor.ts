import { HttpInterceptorFn, HttpErrorResponse, HttpEvent, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, switchMap, throwError, BehaviorSubject, filter, take } from 'rxjs';
import { AuthService } from '../../features/auth/services/auth.service';

// Shared state for token refresh (outside the interceptor function)
let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // Skip auth header for public endpoints
  if (isPublicEndpoint(req.url)) {
    return next(req);
  }

  // Add auth token to request
  const token = authService.getToken();
  if (token) {
    req = addToken(req, token);
  }

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        return handle401Error(req, next, authService);
      }
      return throwError(() => error);
    })
  );
};

/**
 * Add JWT token to request headers
 */
function addToken(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}

/**
 * Check if endpoint is public (doesn't require auth)
 */
function isPublicEndpoint(url: string): boolean {
  const publicEndpoints = [
    '/Auth/login',
    '/Auth/register',
    '/Auth/confirm-email',
    '/Auth/refresh',
    '/Auth/forgot-password',
    '/Auth/reset-password',
    '/Auth/registration-options',
    '/Band' // Public band listing
  ];

  return publicEndpoints.some(endpoint => url.includes(endpoint));
}

/**
 * Handle 401 Unauthorized errors by refreshing token
 */
function handle401Error(
  request: HttpRequest<unknown>, 
  next: HttpHandlerFn, 
  authService: AuthService
): Observable<HttpEvent<unknown>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    return authService.refreshToken().pipe(
      switchMap(() => {
        isRefreshing = false;
        const newToken = authService.getToken();
        refreshTokenSubject.next(newToken);
        return next(addToken(request, newToken || ''));
      }),
      catchError(error => {
        isRefreshing = false;
        authService.logout(); // void method - no subscribe needed
        return throwError(() => error);
      })
    );
  } else {
    // Wait for token refresh to complete
    return refreshTokenSubject.pipe(
      filter(token => token !== null),
      take(1),
      switchMap(token => {
        return next(addToken(request, token || ''));
      })
    );
  }
}