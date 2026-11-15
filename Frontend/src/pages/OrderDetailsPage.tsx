import { useLoad } from "@/hooks/useLoad";
import { useState, useRef, type FormEvent } from "react";
import { format, parseISO } from "date-fns";
import { Link, useParams } from "react-router-dom";
import { toast } from "sonner";
import { orders, rememberOrder } from "@/services/orderService";
import { products } from "@/services/productService";
import { PageHeading, ErrorState, Loading, Status } from "@/components/shared";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { attributes } from "@/types";
import {
  AlertDialog,
  AlertDialogTrigger,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogCancel,
  AlertDialogAction,
} from "@/components/ui/alert-dialog";
import { money } from "@/lib/utils";
import { errorMessage } from "@/lib/api";
export default function OrderDetailsPage({
  management = false,
}: {
  management?: boolean;
}) {
  const { orderId = "" } = useParams(),
    lock = useRef(false);
  const result = useLoad(async () => {
    const o = await orders.get(orderId);
    if (!management) rememberOrder(o.id);
    return o;
  }, [orderId, management]);
  const catalog = useLoad(products.list);
  const [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [open, setOpen] = useState(false),
    [editingCustomer, setEditingCustomer] = useState(false),
    [customerName, setCustomerName] = useState("");
  async function saveCustomer(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (lock.current) return;
    const name = customerName.trim();
    if (!name || name.length > 200) {
      setError("Customer name must contain between 1 and 200 characters.");
      return;
    }
    lock.current = true;
    setBusy(true);
    setError("");
    try {
      await orders.update(orderId, name);
      setEditingCustomer(false);
      result.reload();
      toast.success("Order customer updated");
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      lock.current = false;
      setBusy(false);
    }
  }
  async function cancel() {
    if (lock.current) return;
    lock.current = true;
    setBusy(true);
    setError("");
    try {
      const o = await orders.cancel(orderId);
      if (o.status === "Cancelled") toast.success("Order cancelled");
      else toast.info("Some cancellations need another attempt.");
      setOpen(false);
      result.reload();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      lock.current = false;
      setBusy(false);
    }
  }
  const o = result.data;
  return (
    <>
      <Button variant="ghost" asChild className="mb-5 -ml-3">
        <Link to={management ? "/management/orders" : "/bookings"}>
          {management ? "← All orders" : "← My bookings"}
        </Link>
      </Button>
      {result.loading ? (
        <Loading />
      ) : result.error ? (
        <ErrorState message={result.error} retry={result.reload} />
      ) : (
        o && (
          <>
            <PageHeading
              eyebrow={management ? "Order management" : "Booking details"}
              title={
                management
                  ? "Order details"
                  : o.status === "Confirmed"
                    ? "Your trip is confirmed."
                    : o.status === "Cancelled"
                      ? "Your order is cancelled."
                      : "Your order status."
              }
            >
              Save the order reference below to reopen these booking details.
            </PageHeading>
            <div className="mb-6 flex flex-wrap items-center gap-3">
              <Status value={o.status} />
              <Button variant="outline" size="sm" disabled={busy} onClick={result.reload}>
                Refresh status
              </Button>
            </div>
            {["RequiresAttention", "Pending", "Compensating"].includes(
              o.status,
            ) && (
              <div className="mb-6">
                <ErrorState message="This order is not confirmed. Some supplier outcomes may need review. Refresh its status and keep this reference; avoid making a duplicate booking." />
              </div>
            )}
            {o.status === "Failed" && (
              <div className="mb-6">
                <ErrorState message="Checkout failed. Review the supplier booking statuses below before making new selections." />
              </div>
            )}
            {o.status === "CancellationFailed" && (
              <div className="mb-6">
                <ErrorState message="Some bookings could not be cancelled. You can retry cancellation; bookings already cancelled will be skipped." />
              </div>
            )}
            <div className="grid items-start gap-7 lg:grid-cols-[1.5fr_1fr]">
              <div className="space-y-4">
                <h2>Supplier bookings</h2>
                {o.bookings.map((b) => {
                  const product = catalog.data?.find((p) => p.id === b.productId);
                  const option = attributes(product?.attributesJson).options
                    .find((option) => option.supplierOptionId === b.supplierOptionId);
                  return (
                  <Card className="shadow-none" key={b.id}>
                    <CardContent className="space-y-4">
                      <div className="flex flex-wrap justify-between gap-3">
                        <Link
                          className="font-semibold hover:text-primary"
                          to={`${management ? "/management/products" : "/products"}/${b.productId}`}
                        >
                          {product?.name || `Product ${b.productId}`}
                        </Link>
                        <Status value={b.status} />
                      </div>
                      <p className="text-sm text-muted-foreground">
                        Supplier {b.supplierCode} · Booking {b.id}
                      </p>
                      <Separator />
                      <dl className="grid gap-4 text-sm sm:grid-cols-2">
                        <div>
                          <dt className="text-muted-foreground">
                            {b.activityDate ? "Activity package" : "Room type"}
                          </dt>
                          <dd className="mt-1 font-medium">
                            {option?.name || option?.label || b.supplierOptionId}
                          </dd>
                        </div>
                        <div>
                          <dt className="text-muted-foreground">Guests</dt>
                          <dd className="mt-1">{b.adults} adults · {b.children} children</dd>
                        </div>
                        {b.activityDate ? (
                          <>
                            <div>
                              <dt className="text-muted-foreground">Activity date</dt>
                              <dd className="mt-1">{format(parseISO(b.activityDate), "MMM d, yyyy")}</dd>
                            </div>
                            <div>
                              <dt className="text-muted-foreground">Time slot</dt>
                              <dd className="mt-1">{b.timeSlot || "—"}</dd>
                            </div>
                          </>
                        ) : (
                          <>
                            <div>
                              <dt className="text-muted-foreground">Stay dates</dt>
                              <dd className="mt-1">
                                {b.checkIn ? format(parseISO(b.checkIn), "MMM d, yyyy") : "—"}
                                {" → "}
                                {b.checkOut ? format(parseISO(b.checkOut), "MMM d, yyyy") : "—"}
                              </dd>
                            </div>
                            <div>
                              <dt className="text-muted-foreground">Rooms</dt>
                              <dd className="mt-1">{b.rooms}</dd>
                            </div>
                          </>
                        )}
                        <div>
                          <dt className="text-muted-foreground">Supplier product reference</dt>
                          <dd className="mt-1 break-all font-mono">{b.supplierProductId}</dd>
                        </div>
                        <div>
                          <dt className="text-muted-foreground">Supplier option reference</dt>
                          <dd className="mt-1 break-all font-mono">{b.supplierOptionId}</dd>
                        </div>
                      </dl>
                      <div>
                        <p className="text-xs text-muted-foreground">
                          Supplier confirmation reference
                        </p>
                        <p className="mt-1 break-all font-mono text-sm">
                          {b.supplierBookingId || "Not yet confirmed"}
                        </p>
                      </div>
                      <p className="font-semibold">
                        {money(b.totalPrice, b.currency || o.currency)}
                      </p>
                      {b.errorMessage && (
                        <p className="text-sm text-destructive">
                          This supplier booking needs review. Current status:{" "}
                          {b.status}.
                        </p>
                      )}
                    </CardContent>
                  </Card>
                  );
                })}
              </div>
              <Card className="shadow-none">
                <CardContent className="space-y-5">
                  <h2>Order summary</h2>
                  <dl className="space-y-4 text-sm">
                    <div>
                      <dt className="text-muted-foreground">Order reference</dt>
                      <dd className="mt-1 break-all font-mono">{o.id}</dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">Customer</dt>
                      <dd className="mt-1 font-medium">{o.customerName}</dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">Created</dt>
                      <dd className="mt-1">
                        {new Date(o.createdAt).toLocaleString()}
                      </dd>
                    </div>
                  </dl>
                  {management && (editingCustomer ? (
                    <form onSubmit={saveCustomer} className="space-y-3">
                      <Label htmlFor="order-customer">Order customer name</Label>
                      <Input
                        id="order-customer"
                        value={customerName}
                        onChange={(event) => setCustomerName(event.target.value)}
                        required
                        maxLength={200}
                        disabled={busy}
                        aria-describedby="order-customer-hint"
                      />
                      <p id="order-customer-hint" className="text-xs text-muted-foreground">
                        Updates this order's customer name. Names on supplier reservations stay as originally booked.
                      </p>
                      <div className="flex gap-2">
                        <Button type="submit" disabled={busy}>
                          {busy ? "Saving…" : "Save customer"}
                        </Button>
                        <Button type="button" variant="outline" disabled={busy} onClick={() => {
                          setEditingCustomer(false);
                          setError("");
                        }}>Cancel edit</Button>
                      </div>
                    </form>
                  ) : (
                    <Button variant="outline" disabled={busy} onClick={() => {
                      setCustomerName(o.customerName);
                      setEditingCustomer(true);
                      setError("");
                    }}>Edit customer name</Button>
                  ))}
                  <Separator />
                  <div className="flex justify-between gap-3">
                    <span>Order total</span>
                    <strong>{money(o.totalPrice, o.currency)}</strong>
                  </div>
                  {error && <ErrorState message={error} />}{" "}
                  {["Confirmed", "CancellationFailed"].includes(o.status) && (
                    <AlertDialog
                      open={open}
                      onOpenChange={(v) => {
                        if (!busy) setOpen(v);
                      }}
                    >
                      <AlertDialogTrigger asChild>
                        <Button
                          variant="outline"
                          className="w-full text-destructive"
                          disabled={busy || editingCustomer}
                        >
                          {o.status === "CancellationFailed"
                            ? "Retry cancellation"
                            : "Cancel order"}
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>
                            Cancel this order?
                          </AlertDialogTitle>
                          <AlertDialogDescription>
                            This requests cancellation of all supplier bookings
                            in this order. The result will show whether each
                            supplier confirmed cancellation.
                          </AlertDialogDescription>
                        </AlertDialogHeader>
                        {error && <ErrorState message={error} />}
                        <AlertDialogFooter>
                          <AlertDialogCancel disabled={busy}>
                            Keep order
                          </AlertDialogCancel>
                          <AlertDialogAction
                            disabled={busy}
                            onClick={(e) => {
                              e.preventDefault();
                              void cancel();
                            }}
                          >
                            {busy ? "Cancelling…" : "Request cancellation"}
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                  )}
                </CardContent>
              </Card>
            </div>
          </>
        )
      )}
    </>
  );
}
