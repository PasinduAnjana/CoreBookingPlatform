import { api } from "@/lib/api";
import type { Cart, Quote, Selection } from "@/types";
export const carts = {
  async create() {
    return (await api.post<Cart>("/api/carts")).data;
  },
  async get(id: string) {
    return (await api.get<Cart>(`/api/carts/${id}`)).data;
  },
  async add(id: string, item: Selection) {
    return (await api.post<Cart>(`/api/carts/${id}/items`, item)).data;
  },
  async remove(id: string, item: number) {
    await api.delete(`/api/carts/${id}/items/${item}`);
  },
  async quote(id: string) {
    return (await api.get<Quote>(`/api/carts/${id}/quote`)).data;
  },
};
