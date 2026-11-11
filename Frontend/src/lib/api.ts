import axios from "axios";
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "https://localhost:7000",
  timeout: 60000,
});
export function errorMessage(error: unknown): string {
  if (!axios.isAxiosError(error))
    return "Something went wrong. Please try again.";
  if (!error.response)
    return "Cannot reach Core Booking. Check that the gateway is running and its HTTPS certificate is trusted.";
  const status = error.response.status;
  const data = error.response.data;
  // Only surface short validation text. Server exceptions and stack traces stay hidden.
  if (
    [400, 409].includes(status) &&
    typeof data === "string" &&
    data.length < 240 &&
    !/exception|\bat .*\(|stack trace|<html/i.test(data)
  )
    return data;
  if (status === 400 && data?.errors)
    return (
      Object.values(data.errors)
        .flat()
        .filter((v) => typeof v === "string")
        .join(" ")
        .slice(0, 240) || "Please check your booking details."
    );
  return (
    (
      {
        400: "Please check your booking details.",
        404: "The requested product, cart, or order was not found.",
        409: "This booking cannot be completed in its current state. Refresh availability and try again.",
        502: "A supplier service is unavailable. Please try again shortly.",
        504: "The supplier took too long to respond. Check your order before trying again.",
      } as Record<number, string>
    )[status] ||
    "The service could not complete your request. Please try again."
  );
}
export function isNotFound(error: unknown) {
  return axios.isAxiosError(error) && error.response?.status === 404;
}
