import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { TokenService } from '../services/token.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const tokenService = inject(TokenService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        // Unauthorized - clear tokens and redirect to login
        tokenService.clearTokens();
        router.navigate(['/auth/login']);
      } else if (error.status === 403) {
        // Forbidden
        console.error('Access denied');
      } else if (error.status === 404) {
        // Not found
        console.error('Resource not found');
      } else if (error.status === 500) {
        // Server error
        console.error('Server error occurred');
      }

      return throwError(() => error);
    })
  );
};