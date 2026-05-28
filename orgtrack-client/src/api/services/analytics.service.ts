import { api } from '../axios';

export interface MemberActivityScoreDto {
  userId: string;
  userName: string;
  tasksDone: number;
  eventsAttended: number;
  totalScore: number;
}

export interface ActivityLogDto {
  createdAt: string;
  action: string;
  entityType: string;
  details: string | null;
}

export interface UnitActivitySummaryDto {
  unitId: string;
  unitName: string;
  tasksDone: number;
  eventsHeld: number;
  membersActive: number;
  recentLogs: ActivityLogDto[];
}

export const analyticsService = {
  async getMemberScore(userId: string): Promise<MemberActivityScoreDto> {
    const response = await api.get<MemberActivityScoreDto>(`/analytics/members/${userId}`, {
      _skipForbiddenRedirect: true
    } as any);
    return response.data;
  },

  async getUnitSummary(unitId: string): Promise<UnitActivitySummaryDto> {
    const response = await api.get<UnitActivitySummaryDto>(`/analytics/units/${unitId}`);
    return response.data;
  },

  async getNationalDashboard(nationalUnitId: string): Promise<UnitActivitySummaryDto[]> {
    const response = await api.get<UnitActivitySummaryDto[]>(`/analytics/national/${nationalUnitId}`);
    return response.data;
  }
};
