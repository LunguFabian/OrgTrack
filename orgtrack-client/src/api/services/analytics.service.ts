import { api } from '../axios';

export interface MemberActivityScoreDto {
  userId: string;
  userName: string;
  unitName?: string;
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
  /** Fetch individual member score */
  async getMemberScore(userId: string): Promise<MemberActivityScoreDto> {
    const response = await api.get<MemberActivityScoreDto>(`/analytics/members/${userId}`, {
      _skipForbiddenRedirect: true
    } as any);
    return response.data;
  },

  /** Fetch the entire leaderboard for a unit */
  async getLeaderboard(unitId: string): Promise<MemberActivityScoreDto[]> {
    const response = await api.get<MemberActivityScoreDto[]>(`/analytics/units/${unitId}/leaderboard`);
    return response.data;
  },

  async getUnitSummary(unitId: string): Promise<UnitActivitySummaryDto> {
    const response = await api.get<UnitActivitySummaryDto>(`/analytics/units/${unitId}`);
    return response.data;
  },

  async getNationalDashboard(nationalUnitId: string): Promise<UnitActivitySummaryDto[]> {
    const response = await api.get<UnitActivitySummaryDto[]>(`/analytics/national/${nationalUnitId}`);
    return response.data;
  },

  async downloadReport(unitId: string, format: 'pdf' | 'excel'): Promise<Blob> {
    const response = await api.get(`/analytics/units/${unitId}/report`, {
      params: { format },
      responseType: 'blob'
    });
    return response.data;
  }
};
