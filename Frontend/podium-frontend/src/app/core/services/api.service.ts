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
  // FIX 1: Use a getter to ensure we always get the fresh value from environment
  // This prevents initialization race conditions where baseUrl might be undefined
  protected get baseUrl(): string {
    return environment.apiUrl;
  }

  constructor(protected http: HttpClient) {
    console.log('✅ ApiService Ready. Target API:', this.baseUrl);
  }

  /**
   * GET request
   */
  get<T>(endpoint: string, params?: any): Observable<T> {
  const fullUrl = `${this.baseUrl}/${endpoint}`;
  console.log(`🚀 API SENDING REQUEST TO: ${fullUrl}`); // <--- LOOK FOR THIS LOG
  return this.http.get<T>(fullUrl, { params: this.buildHttpParams(params) }).pipe(
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
   * Upload file
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

  protected handleError(error: HttpErrorResponse): Observable<never> {
    const apiError: ApiError = {
      message: 'An error occurred',
      statusCode: error.status,
      timestamp: new Date()
    };

    if (error.error instanceof ErrorEvent) {
      apiError.message = error.error.message;
    } else {
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