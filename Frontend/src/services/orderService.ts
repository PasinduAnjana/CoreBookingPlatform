import { AxiosError } from "axios";
import { api } from "@/lib/api";
import { isOrder } from "@/types";
function checked(data: unknown) {
  if (!isOrder(data)) throw new Error("Invalid order response");
  return data;
}
export const orders = {
  async get(id: string) {
    return checked(
      (await api.get(`/api/orders/${encodeURIComponent(id)}`)).data,
    );
  },
  async checkout(id: string, customerName: string) {
    const response = await api.post(
      `/api/carts/${id}/checkout`,
      { customerName },
      { validateStatus: (s) => (s >= 200 && s < 300) || s === 409 },
    );
    if (!isOrder(response.data))
      throw new AxiosError(
        "Unexpected checkout response",
        undefined,
        response.config,
        response.request,
        response,
      );
    return response.data;
  },
  async cancel(id: string) {
    return checked(
      (await api.post(`/api/orders/${encodeURIComponent(id)}/cancel`)).data,
    );
  },
};
export function savedOrders(): string[] {
  try {
    const v = JSON.parse(localStorage.getItem("core-booking.orders") || "[]");
    return Array.isArray(v)
      ? v.filter((s: unknown) => typeof s === "string").slice(0, 50)
      : [];
  } catch {
    return [];
  }
}
export function rememberOrder(id: string) {
  try {
    localStorage.setItem(
      "core-booking.orders",
      JSON.stringify(
        [id, ...savedOrders().filter((v) => v !== id)].slice(0, 50),
      ),
    );
  } catch {
    /* Storage may be disabled. Lookup still works. */
  }
}
