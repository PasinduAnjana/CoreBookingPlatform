import { api, isNotFound } from "@/lib/api";
import type { Product, Content } from "@/types";
export const products = {
  async list() {
    const { data } = await api.get<Product[]>("/api/product");
    if (!Array.isArray(data)) throw new Error("Invalid catalog");
    return data;
  },
  async get(id: number) {
    return (await api.get<Product>(`/api/product/${id}`)).data;
  },
  async content(id: number) {
    try {
      return (await api.get<Content>(`/api/products/${id}/content`)).data;
    } catch (e) {
      if (isNotFound(e)) return null;
      throw e;
    }
  },
};
