import { api, isNotFound } from "@/lib/api";
import type { Product, Content } from "@/types";
export const products = {
  async availableRooms(
    product: Product,
    optionId: string,
    checkIn: string,
    checkOut: string,
  ) {
    const { data } = await api.post<{ availableQuantity: number }>("/api/integration/availability", {
      supplierCode: product.supplierCode,
      supplierProductId: product.supplierProductId,
      supplierOptionId: optionId,
      checkIn,
      checkOut,
      // Probe inventory independently of the form's guest and room counts.
      adults: 1,
      children: 0,
      rooms: 1,
    });
    if (!Number.isSafeInteger(data.availableQuantity) || data.availableQuantity < 0)
      throw new Error("Invalid room availability received from the supplier.");
    return data.availableQuantity;
  },
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
