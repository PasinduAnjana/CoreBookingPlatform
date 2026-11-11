export interface Product {
  id: number;
  name: string;
  location?: string;
  categoryId?: number | null;
  category?: { id: number; name: string };
  lastUpdated?: string | null;
  supplierCode: string;
  supplierProductId: string;
  attributesJson?: string;
  isActive: boolean;
}
export interface Content {
  productId: number;
  description?: string;
  images: { url: string; caption?: string | null; isPrimary: boolean }[];
  highlights: string[];
  facilities: string[];
  inclusions: string[];
  exclusions: string[];
  additionalInfoJson?: string | null;
}
export interface Option {
  supplierOptionId: string;
  name?: string;
  label?: string;
  maxAdults?: number;
  maxChildren?: number;
  bedType?: string;
  maxParticipants?: number;
  passengerTypes?: string[];
}
export interface Attributes {
  options: Option[];
  timeSlots: string[];
  durationMinutes?: number;
}
export function attributes(json?: string): Attributes {
  try {
    const value = JSON.parse(json || "{}");
    return {
      options: Array.isArray(value.options)
        ? value.options.filter(
            (o: Option) =>
              o &&
              typeof o.supplierOptionId === "string" &&
              o.supplierOptionId.length > 0,
          )
        : [],
      timeSlots: Array.isArray(value.timeSlots)
        ? value.timeSlots.filter((s: unknown) => typeof s === "string")
        : [],
      durationMinutes:
        typeof value.durationMinutes === "number"
          ? value.durationMinutes
          : undefined,
    };
  } catch {
    return { options: [], timeSlots: [] };
  }
}
export interface Selection {
  productId: number;
  supplierOptionId: string;
  checkIn?: string;
  checkOut?: string;
  activityDate?: string;
  timeSlot?: string;
  adults: number;
  children: number;
  rooms: number;
}
export interface CartItem extends Selection {
  id: number;
}
export interface Cart {
  id: string;
  createdAt: string;
  items: CartItem[];
}
export interface ItemQuote {
  cartItemId: number;
  productId: number;
  productName: string;
  supplierCode: string;
  supplierOptionId: string;
  available: boolean;
  availableQuantity: number;
  currency: string;
  totalPrice: number;
}
export interface Quote {
  cartId: string;
  items: ItemQuote[];
  allAvailable: boolean;
  currency?: string;
  totalPrice?: number | null;
  quotedAt: string;
}
export interface Booking {
  id: number;
  productId: number;
  supplierCode: string;
  supplierBookingId?: string;
  status: string;
  totalPrice: number;
  errorMessage?: string;
}
export interface Order {
  id: string;
  cartId: string;
  customerName: string;
  status: string;
  currency: string;
  totalPrice: number;
  errorMessage?: string;
  createdAt: string;
  bookings: Booking[];
}
export function isOrder(value: unknown): value is Order {
  if (!value || typeof value !== "object") return false;
  const o = value as Order;
  return (
    typeof o.id === "string" &&
    typeof o.status === "string" &&
    Array.isArray(o.bookings)
  );
}
