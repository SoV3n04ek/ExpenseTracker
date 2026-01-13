export interface AuthResponseDto {
  token: string;
  expiration: string;
  userId: string;
  email: string;
  name: string;
  message?: string;
}

export interface LoginDto {
  email: string;
  password?: string; // Optional if using external providers later
}

export interface RegisterDto {
  email: string;
  password?: string;
  name: string;
}

export interface AuthState {
  user: AuthResponseDto | null;
  isLoading: boolean;
  error: string | null;
}