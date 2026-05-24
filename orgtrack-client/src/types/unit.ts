export interface UnitMemberDto {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  roleName: string;
  joinedAt: string;
  unitName?: string;
  unitId?: string;
}

export interface InviteLinkCreatedDto {
  token: string;
  inviteUrl: string;
  expiresAt: string;
}

export interface TaskDto {
  id: string;
  title: string;
  description: string;
  status: 'ToDo' | 'InProgress' | 'WaitingForApproval' | 'Done';
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  deadline?: string;
  organizationUnitId: string;
  assigneeName?: string;
  assigneeId?: string;
  creatorName: string;
  createdAt: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string;
  priority: string;
  assigneeId?: string | null;
  deadline?: string | null;
}

export interface UpdateTaskRequest {
  title: string;
  description: string;
  priority: string;
  assigneeId?: string | null;
  deadline?: string | null;
}

export interface EventDto {
  id: string;
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  isRecurring: boolean;
  recurrencePattern?: string;
  organizationUnitId: string;
  createdAt: string;
  currentUserRsvp?: string;
}

export interface CreateEventRequest {
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  isRecurring: boolean;
  recurrencePattern?: string;
  externalCalendarId?: string;
  invitedUnitIds: string[];
  invitedUserIds: string[];
}

export interface UpdateEventRequest {
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  isRecurring: boolean;
  recurrencePattern?: string;
  externalCalendarId?: string;
  invitedUnitIds: string[];
  invitedUserIds: string[];
}
