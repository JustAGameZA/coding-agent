/**
 * Admin-related models for user management
 */

export interface UserListItem {
  id: string;
  username: string;
  email: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export interface UserListResponse {
  users: UserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface UpdateUserRolesRequest {
  roles: string[];
}

export interface InfrastructureLink {
  name: string;
  url: string;
  icon: string;
  description: string;
}
