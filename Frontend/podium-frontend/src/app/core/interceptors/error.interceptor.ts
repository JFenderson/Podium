import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { MatSnackBar } from '@angular/material/snack-bar';
import { environment } from '../../../../environments/environment';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(private snackBar: MatSnackBar) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        let errorMessage = 'An error occurred';

        if (error.error instanceof ErrorEvent) {
          // Client-side error
          errorMessage = `Error: ${error.error.message}`;
        } else {
          // Server-side error
          switch (error.status) {
            case 400:
              errorMessage = this.handleBadRequest(error);
              break;
            case 401:
              errorMessage = 'Unauthorized. Please log in.';
              break;
            case 403:
              errorMessage = 'Access denied. You do not have permission to perform this action.';
              break;
            case 404:
              errorMessage = 'Resource not found.';
              break;
            case 409:
              errorMessage = error.error?.message || 'A conflict occurred.';
              break;
            case 422:
              errorMessage = this.handleValidationError(error);
              break;
            case 500:
              errorMessage = 'Internal server error. Please try again later.';
              break;
            case 503:
              errorMessage = 'Service unavailable. Please try again later.';
              break;
            default:
              errorMessage = error.error?.message || `Error: ${error.status}`;
          }
        }

        // Show error notification (except for 401 as auth interceptor handles it)
        if (error.status !== 401) {
          this.showError(errorMessage);
        }

        if (environment.enableDebugLogging) {
          console.error('HTTP Error:', {
            status: error.status,
            message: errorMessage,
            url: request.url,
            error: error
          });
        }

        return throwError(() => error);
      })
    );
  }

  /**
   * Handle 400 Bad Request errors
   */
  private handleBadRequest(error: HttpErrorResponse): string {
    if (error.error?.errors) {
      // Validation errors from backend
      const validationErrors = error.error.errors;
      const errorMessages = Object.keys(validationErrors)
        .map(key => validationErrors[key].join(', '))
        .join('; ');
      return errorMessages;
    }
    return error.error?.message || 'Bad request';
  }

  /**
   * Handle 422 Validation errors
   */
  private handleValidationError(error: HttpErrorResponse): string {
    if (error.error?.errors) {
      const validationErrors = error.error.errors;
      const errorMessages = Object.keys(validationErrors)
        .map(key => `${key}: ${validationErrors[key].join(', ')}`)
        .join('; ');
      return errorMessages;
    }
    return error.error?.message || 'Validation failed';
  }

  /**
   * Show error message to user
   */
  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'end',
      verticalPosition: 'top',
      panelClass: ['error-snackbar']
    });
  }
}