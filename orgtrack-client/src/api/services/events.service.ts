import { api } from '../axios';
import type { EventDto, CreateEventRequest, UpdateEventRequest } from '../../types/unit';

export interface AttendanceReportItem {
  userId: string;
  userName: string;
  rsvp: string;
  attendance: string;
}

export interface RsvpSummaryItem {
  userId: string;
  userName: string;
  rsvp: string;
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

  async updateEvent(unitId: string, eventId: string, data: UpdateEventRequest): Promise<EventDto> {
    const res = await api.put<EventDto>(`/organization/units/${unitId}/events/${eventId}`, data);
    return res.data;
  },

  async deleteEvent(unitId: string, eventId: string): Promise<void> {
    await api.delete(`/organization/units/${unitId}/events/${eventId}`);
  },

  async rsvp(unitId: string, eventId: string, status: 'Going' | 'Maybe' | 'NotGoing'): Promise<void> {
    await api.post(`/organization/units/${unitId}/events/${eventId}/rsvp`, { status });
  },

  async getMyEvents(): Promise<EventDto[]> {
    const res = await api.get<EventDto[]>('/me/events');
    return res.data;
  },

  async getRsvpSummary(unitId: string, eventId: string): Promise<RsvpSummaryItem[]> {
    const res = await api.get<RsvpSummaryItem[]>(`/organization/units/${unitId}/events/${eventId}/rsvp-summary`);
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
