import { api } from "@/lib/api";
import { isOrder, type Content, type Order } from "@/types";
export interface Category {
  id: number;
  name: string;
  description: string;
}
export interface ImportSummary {
  supplierCode: string;
  total: number;
  created: number;
  updated: number;
  failed: number;
  errors: string[];
}
export interface OrderPage {
  items: Order[];
  totalCount: number;
  page: number;
  pageSize: number;
}
export type ContentInput = Omit<Content, "productId">;
export const management = {
  async categories() {
    const { data } = await api.get<Category[]>("/api/category");
    if (!Array.isArray(data)) throw new Error("Invalid categories");
    return data;
  },
  async saveCategory(value: Omit<Category, "id">, id?: number) {
    if (id) await api.put(`/api/category/${id}`, value);
    else await api.post("/api/category", value);
  },
  async deleteCategory(id: number) {
    await api.delete(`/api/category/${id}`);
  },
  async updateProduct(
    id: number,
    value: { categoryId: number | null; isActive: boolean },
  ) {
    await api.put(`/api/product/${id}`, value);
  },
  async saveContent(id: number, value: ContentInput) {
    await api.put(`/api/products/${id}/content`, value);
  },
  async deleteContent(id: number) {
    await api.delete(`/api/products/${id}/content`);
  },
  async sync(supplier: "ABC" | "CDE") {
    const { data } = await api.post<ImportSummary>(
      `/api/integration/import/${supplier}`,
      undefined,
      { timeout: 120000 },
    );
    if (
      typeof data?.created !== "number" ||
      typeof data?.updated !== "number" ||
      typeof data?.failed !== "number"
    )
      throw new Error("Invalid import result");
    return data;
  },
  async orders(page: number) {
    const { data } = await api.get<OrderPage>("/api/orders", {
      params: { page, pageSize: 20 },
    });
    if (
      !Array.isArray(data?.items) ||
      !data.items.every(isOrder) ||
      typeof data.totalCount !== "number"
    )
      throw new Error("Invalid order list");
    return data;
  },
};
