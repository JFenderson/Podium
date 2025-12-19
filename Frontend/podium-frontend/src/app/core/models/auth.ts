// Auth Models - Complete and Updated
// File: src/app/core/models/auth.models.ts

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  role: string;  // 'Student', 'Guardian', 'BandStaff', 'Director'
}

export interface LoginResponse {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  permissions?: string[];
  token: string;
  refreshToken: string;
  tokenExpiration: string | Date;
  
  // Optional role-specific IDs
  studentId?: number;
  guardianId?: number;
  bandStaffId?: number;
  directorId?: number;
  bandId?: number;
}

export interface CurrentUser {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  permissions: string[];
  token: string;
  refreshToken: string;
  tokenExpiration: Date;
  
  // Optional role-specific IDs
  studentId?: number;
  guardianId?: number;
  bandStaffId?: number;
  directorId?: number;
  bandId?: number;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}