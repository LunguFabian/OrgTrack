import type { UnitMemberDto } from './unit';

export interface OrganizationUnitDto {
  id: string;
  name: string;
  description: string;
  type: string;
  departmentType: string;
  parentUnitId: string | null;
  createdAt: string;
  children: OrganizationUnitDto[];
  childrenCount: number;
  members: UnitMemberDto[];
}

export interface CreateUnitRequest {
  name: string;
  description: string;
  type: string; // "National", "Committee", "Department", "Team"
  departmentType?: string | null;
  parentUnitId: string | null;
}
