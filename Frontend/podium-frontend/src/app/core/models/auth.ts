// Authentication DTOs matching backend

export interface RegisterDto {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  role: string;
  phoneNumber?: string;
  bandId?: number; // For BandStaff/Director roles
  studentIds?: number[]; // For Guardian role
}

export interface LoginDto {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: Date;
  userId: string;
  email: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface AuthResult {
  success: boolean;
  errors?: string[];
  accessToken?: string;
  refreshToken?: string;
  expiresAt?: Date;
  userId?: string;
  email?: string;
}

export interface CurrentUser {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  permissions: string[];
  
  // Add these
  studentId?: number;
  guardianId?: number;
  bandStaffId?: number;  
  directorId?: number;
  bandId?: number;
  
  token: string;
  refreshToken: string;
  tokenExpiration: Date;

}

export interface RegistrationOptions {
  bands: BandOption[];
  roles: string[];
}

export interface BandOption {
  id: number;
  bandName: string;
  state: string;
}