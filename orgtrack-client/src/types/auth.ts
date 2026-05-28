export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  pictureUrl?: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  user: User;
}
