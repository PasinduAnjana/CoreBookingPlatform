import { useRef, useState, type FormEvent } from "react";
import { useNavigate, Link } from "react-router-dom";
import { toast } from "sonner";
import { orders, rememberOrder } from "@/services/orderService";
import { useCart } from "@/contexts/CartContext";
import { CartView } from "@/components/CartView";
import { PageHeading, ErrorState } from "@/components/shared";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { errorMessage } from "@/lib/api";
function ConfirmForm() {
  const ctx = useCart(),
    navigate = useNavigate(),
    lock = useRef(false);
  const [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (lock.current || !ctx.cartId) return;
    const name = String(new FormData(e.currentTarget).get("name") || "").trim();
    if (!name) {
      setError("Enter the customer name.");
      return;
    }
    lock.current = true;
    setBusy(true);
    setError("");
    try {
      const order = await orders.checkout(ctx.cartId, name);
      rememberOrder(order.id);
      ctx.release();
      if (order.status === "Confirmed")
        toast.success("Your booking is confirmed");
      else if (order.status === "Failed")
        toast.error("Your booking could not be completed");
      else
        toast.info(
          "Your order needs a status check. Please review the details.",
        );
      navigate(`/orders/${order.id}`);
    } catch (e) {
      setError(
        `${errorMessage(e)} If you lost the response, retry with this same cart; do not create a second cart.`,
      );
    } finally {
      setBusy(false);
      lock.current = false;
    }
  }
  return (
    <form onSubmit={submit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="name">Customer name</Label>
        <Input
          id="name"
          name="name"
          autoComplete="name"
          placeholder="Full name"
          maxLength={200}
          required
          disabled={busy}
        />
      </div>
      {error && <ErrorState message={error} />}
      <Button className="w-full" type="submit" disabled={busy}>
        {busy ? "Confirming with suppliers…" : "Confirm booking"}
      </Button>
      <p className="text-xs leading-relaxed text-muted-foreground">
        This creates bookings with the suppliers in your cart. Keep this page
        open until the result is returned.
      </p>
    </form>
  );
}
export default function CheckoutPage() {
  return (
    <>
      <Button variant="ghost" asChild className="mb-5 -ml-3">
        <Link to="/cart">← Back to cart</Link>
      </Button>
      <PageHeading eyebrow="Final step" title="Confirm your booking.">
        Review the live quote and enter the customer name.
      </PageHeading>
      <CartView checkout>{() => <ConfirmForm />}</CartView>
    </>
  );
}
