import { api } from '../axios';
import type { TaskDto, CreateTaskRequest, UpdateTaskRequest, WorkloadScoreDto } from '../../types/unit';

export const tasksService = {
  async getTasksByUnit(unitId: string): Promise<TaskDto[]> {
    const res = await api.get<TaskDto[]>(`/organization/units/${unitId}/tasks`);
    return res.data;
  },

  async getUnitWorkload(unitId: string): Promise<WorkloadScoreDto[]> {
    const res = await api.get<WorkloadScoreDto[]>(`/organization/units/${unitId}/tasks/workload`);
    return res.data;
  },

  async getMyTasks(): Promise<TaskDto[]> {
    const res = await api.get<TaskDto[]>('/me/tasks');
    return res.data;
  },

  async createTask(unitId: string, data: CreateTaskRequest): Promise<TaskDto> {
    const res = await api.post<TaskDto>(`/organization/units/${unitId}/tasks`, data);
    return res.data;
  },

  async updateTask(unitId: string, taskId: string, data: UpdateTaskRequest): Promise<TaskDto> {
    const res = await api.put<TaskDto>(`/organization/units/${unitId}/tasks/${taskId}`, data);
    return res.data;
  },

  async updateTaskStatus(unitId: string, taskId: string, newStatus: string): Promise<TaskDto> {
    const res = await api.patch<TaskDto>(`/organization/units/${unitId}/tasks/${taskId}/status`, { newStatus });
    return res.data;
  },

  async deleteTask(unitId: string, taskId: string): Promise<void> {
    await api.delete(`/organization/units/${unitId}/tasks/${taskId}`);
  }
};
