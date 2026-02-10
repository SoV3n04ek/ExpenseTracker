export interface AuthResponseDto {
  token: string;
  expiration: string;
  userId: string;
  email: string;
  name: string;
  message?: string;
  errors?: string[];
}

export interface LoginDto {
  email: string;
  password?: string; // Optional if using external providers later
}

export interface RegisterDto {
  email: string;
  password?: string;
  confirmPassword?: string;
  name: string;
}

export interface AuthState {
  user: AuthResponseDto | null;
  isLoading: boolean;
  error: string | string[] | null;
  status: 'idle' | 'loading' | 'unconfirmed' | 'error' | 'confirmed';
  unconfirmedEmail: string | null;
}
export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}
