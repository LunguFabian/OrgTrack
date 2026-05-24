import { api } from '../axios';
import type { InviteLinkCreatedDto } from '../../types/unit';

export const inviteService = {
  async generateLink(data: {
    organizationUnitId: string;
    roleName: string;
    expiresInHours: number;
    maxUses: number;
  }): Promise<InviteLinkCreatedDto> {
    const res = await api.post<InviteLinkCreatedDto>('/invite-links', data);
    return res.data;
  },

  async getInviteDetails(token: string) {
    const res = await api.get(`/invite-links/${token}`);
    return res.data;
  },

  async joinViaLink(token: string) {
    const res = await api.post(`/invite-links/${token}/join`);
    return res.data;
  }
};
