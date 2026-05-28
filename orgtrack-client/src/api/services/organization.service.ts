import { api } from '../axios';
import type { OrganizationUnitDto, CreateUnitRequest } from '../../types/organization';

export const organizationService = {
  /** Fetch the entire pre-calculated organizational tree from the backend */
  async getFullTree(): Promise<OrganizationUnitDto[]> {
    const response = await api.get<OrganizationUnitDto[]>('/organization/tree');
    return response.data;
  },

  /** Fetch the units that the current user belongs to */
  async getMyUnits(): Promise<OrganizationUnitDto[]> {
    const response = await api.get<OrganizationUnitDto[]>('/me/units');
    return response.data;
  },

  /** Create a new node (unit) in the tree */
  async createUnit(data: CreateUnitRequest): Promise<OrganizationUnitDto> {
    const response = await api.post<OrganizationUnitDto>('/organization/units', data);
    return response.data;
  },

  /** Fetch the details of a specific unit */
  async getUnitById(id: string): Promise<OrganizationUnitDto> {
    const response = await api.get<OrganizationUnitDto>(`/organization/units/${id}`);
    return response.data;
  },

  /** Search for members in the organization by name or email */
  async searchMembers(query: string): Promise<Array<{id: string, firstName: string, lastName: string, email: string, pictureUrl?: string}>> {
    const response = await api.get(`/organization/search-members?q=${encodeURIComponent(query)}`);
    return response.data;
  },

  /** Fetch the public profile of any user */
  async getUserProfile(userId: string): Promise<{
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    pictureUrl?: string;
    units: Array<{ id: string; name: string; type: string; departmentType: string; roleName: string }>;
  }> {
    const response = await api.get(`/organization/users/${userId}/profile`);
    return response.data;
  }
};
