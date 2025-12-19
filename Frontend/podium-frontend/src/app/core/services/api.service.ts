import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import { ServiceResult, ApiError } from '../models/common.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  protected readonly baseUrl = environment.apiUrl;

  constructor(protected http: HttpClient) {}

  /**
   * GET request
   */
  get<T>(endpoint: string, params?: any): Observable<T> {
    const httpParams = this.buildHttpParams(params);
    
    return this.http.get<T>(`${this.baseUrl}/${endpoint}`, { params: httpParams }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * POST request
   */
  post<T>(endpoint: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${endpoint}`, body).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * PUT request
   */
  put<T>(endpoint: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}/${endpoint}`, body).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * PATCH request
   */
  patch<T>(endpoint: string, body: any): Observable<T> {
    return this.http.patch<T>(`${this.baseUrl}/${endpoint}`, body).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * DELETE request
   */
  delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}/${endpoint}`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Upload file with form data
   */
  upload<T>(endpoint: string, formData: FormData): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${endpoint}`, formData).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Download file
   */
  download(endpoint: string, params?: any): Observable<Blob> {
    const httpParams = this.buildHttpParams(params);
    
    return this.http.get(`${this.baseUrl}/${endpoint}`, {
      params: httpParams,
      responseType: 'blob'
    }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Handle ServiceResult wrapper
   */
  handleServiceResult<T>(endpoint: string, params?: any): Observable<T> {
    return this.get<ServiceResult<T>>(endpoint, params).pipe(
      map(result => {
        if (result.isSuccess && result.data) {
          return result.data;
        }
        throw new Error(result.errorMessage || 'Request failed');
      })
    );
  }

  /**
   * Build HTTP params from object
   */
  private buildHttpParams(params?: any): HttpParams {
    let httpParams = new HttpParams();
    
    if (params) {
      Object.keys(params).forEach(key => {
        const value = params[key];
        if (value !== null && value !== undefined) {
          if (Array.isArray(value)) {
            value.forEach(item => {
              httpParams = httpParams.append(key, item.toString());
            });
          } else {
            httpParams = httpParams.append(key, value.toString());
          }
        }
      });
    }
    
    return httpParams;
  }

  /**
   * Handle HTTP errors
   */
  protected handleError(error: HttpErrorResponse): Observable<never> {
    const apiError: ApiError = {
      message: 'An error occurred',
      statusCode: error.status,
      timestamp: new Date()
    };

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      apiError.message = error.error.message;
    } else {
      // Server-side error
      if (error.error?.message) {
        apiError.message = error.error.message;
      } else if (error.error?.errors) {
        apiError.errors = error.error.errors;
        apiError.message = 'Validation failed';
      } else if (error.message) {
        apiError.message = error.message;
      } else {
        apiError.message = `Server error: ${error.status}`;
      }
    }

    if (environment.enableDebugLogging) {
      console.error('API Error:', apiError);
      console.error('Full error object:', error);
    }

    return throwError(() => apiError);
  }
}