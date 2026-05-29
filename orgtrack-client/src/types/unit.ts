export interface UnitMemberDto {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  roleName: string;
  joinedAt: string;
  unitName?: string;
  unitId?: string;
  pictureUrl?: string;
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
  assigneeProfilePictureUrl?: string;
  creatorName: string;
  createdAt: string;
  parentTaskId?: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string;
  priority: string;
  assigneeId?: string | null;
  deadline?: string | null;
  parentTaskId?: string | null;
  status?: string;
}

export interface UpdateTaskRequest {
  title: string;
  description: string;
  priority: string;
  assigneeId?: string | null;
  deadline?: string | null;
  parentTaskId?: string | null;
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
  invitedUnitIds?: string[];
  invitedUserIds?: string[];
  externalCalendarId?: string;
  createdAt: string;
  currentUserRsvp?: string;
}

export interface CreateEventRequest {
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  isRecurring: boolean;
  recurrencePattern?: string | null;
  externalCalendarId?: string | null;
  invitedUnitIds: string[];
  invitedUserIds: string[];
}

export interface UpdateEventRequest {
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  isRecurring: boolean;
  recurrencePattern?: string | null;
  externalCalendarId?: string | null;
  invitedUnitIds: string[];
  invitedUserIds: string[];
}
