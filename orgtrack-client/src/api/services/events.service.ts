import { api } from '../axios';

export interface EventDto {
  id: string;
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  organizationUnitId: string;
  invitedUnitIds: string[];
  invitedUserIds: string[];
  isRecurring: boolean;
  recurrencePattern: string | null;
  currentUserRsvp?: string;
}

export interface AttendanceReportItem {
  userId: string;
  userName: string;
  status: string;
}

export interface CreateEventRequest {
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  isRecurring: boolean;
  recurrencePattern: string | null;
  externalCalendarId: string | null;
  invitedUnitIds: string[];
  invitedUserIds: string[];
}

export const eventsService = {
  async getEvents(unitId: string): Promise<EventDto[]> {
    const res = await api.get<EventDto[]>(`/organization/units/${unitId}/events`);
    return res.data;
  },

  async createEvent(unitId: string, data: CreateEventRequest): Promise<EventDto> {
    const res = await api.post<EventDto>(`/organization/units/${unitId}/events`, data);
    return res.data;
  },

  async rsvp(unitId: string, eventId: string, status: 'Present' | 'Absent' | 'Maybe'): Promise<void> {
    await api.post(`/organization/units/${unitId}/events/${eventId}/rsvp`, { status });
  },

  async getMyEvents(): Promise<EventDto[]> {
    const res = await api.get<EventDto[]>('/me/events');
    return res.data;
  },

  async getAttendanceReport(unitId: string, eventId: string): Promise<AttendanceReportItem[]> {
    const res = await api.get<AttendanceReportItem[]>(`/organization/units/${unitId}/events/${eventId}/attendance`);
    return res.data;
  },

  async confirmAttendance(unitId: string, eventId: string, targetUserId: string, status: string): Promise<void> {
    await api.post(`/organization/units/${unitId}/events/${eventId}/attendance/${targetUserId}`, { status });
  }
};
