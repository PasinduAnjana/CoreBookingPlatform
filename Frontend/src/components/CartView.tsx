import { useLoad } from "@/hooks/useLoad";
import { useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { Trash2, CalendarDays, Users } from "lucide-react";
import { toast } from "sonner";
import { useCart } from "@/contexts/CartContext";
import { carts } from "@/services/cartService";
import { products } from "@/services/productService";
import { Loading, Empty, ErrorState, Status } from "@/components/shared";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { money } from "@/lib/utils";
import { errorMessage } from "@/lib/api";
import { attributes, type Cart, type Quote } from "@/types";
function canCheckout(q: Quote | null) {
  return (
    !!q &&
    q.items.length > 0 &&
    q.allAvailable &&
    q.items.every((i) => i.available) &&
    typeof q.totalPrice === "number" &&
    Number.isFinite(q.totalPrice) &&
    !!q.currency
  );
}
export function CartView({
  checkout = false,
  children,
}: {
  checkout?: boolean;
  children?: (quote: Quote, cart: Cart) => ReactNode;
}) {
  const ctx = useCart(),
    id = ctx.cartId;
  const [removing, setRemoving] = useState<number | null>(null);
  const result = useLoad(async () => {
    if (!id) return null;
    const cart = await ctx.refresh();
    if (!cart) return null;
    const catalog = await products.list();
    let quote: Quote | null = null,
      quoteError = "";
    if (cart.items.length) {
      try {
        quote = await carts.quote(id);
      } catch (e) {
        quoteError = errorMessage(e);
      }
    }
    return { cart, catalog, quote, quoteError };
  }, [id]);
  async function remove(item: number) {
    setRemoving(item);
    try {
      await ctx.remove(item);
      result.reload();
      toast.success("Item removed");
    } catch (e) {
      toast.error(errorMessage(e));
    } finally {
      setRemoving(null);
    }
  }
  if (!id)
    return (
      <Empty title="Your cart is empty">
        Choose a stay or experience to begin.
        <Button className="mt-5" asChild>
          <Link to="/">Explore products</Link>
        </Button>
      </Empty>
    );
  if (result.loading) return <Loading />;
  if (result.error)
    return <ErrorState message={result.error} retry={result.reload} />;
  const data = result.data;
  if (!data || !data.cart.items.length)
    return (
      <Empty title="Your cart is empty">
        Add a hotel or activity to start your trip.
        <Button asChild className="mt-5">
          <Link to="/">Explore products</Link>
        </Button>
      </Empty>
    );
  const { cart, catalog, quote, quoteError } = data;
  return (
    <div className="grid items-start gap-7 lg:grid-cols-[1.6fr_1fr]">
      <div className="space-y-4">
        {cart.items.map((item) => {
          const p = catalog.find((p) => p.id === item.productId),
            option = attributes(p?.attributesJson).options.find(
              (o) => o.supplierOptionId === item.supplierOptionId,
            ),
            q = quote?.items.find((q) => q.cartItemId === item.id);
          return (
            <Card className="shadow-none" key={item.id}>
              <CardContent>
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <Link
                      className="font-semibold hover:text-primary"
                      to={`/products/${item.productId}`}
                    >
                      {p?.name || q?.productName || `Product ${item.productId}`}
                    </Link>
                    <p className="mt-1 text-sm text-muted-foreground">
                      {option?.name || option?.label || item.supplierOptionId}
                      {p?.location ? ` · ${p.location}` : ""}
                    </p>
                  </div>
                  {!checkout && (
                    <Button
                      variant="ghost"
                      size="icon"
                      aria-label={`Remove ${p?.name || "item"}`}
                      disabled={removing !== null}
                      onClick={() => remove(item.id)}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  )}
                </div>
                <div className="my-4 space-y-2 text-sm text-muted-foreground">
                  <p className="flex items-center gap-2">
                    <CalendarDays className="size-4" />
                    {item.checkIn
                      ? `${item.checkIn} → ${item.checkOut}`
                      : `${item.activityDate} · ${item.timeSlot}`}
                  </p>
                  <p className="flex items-center gap-2">
                    <Users className="size-4" />
                    {item.adults} adults · {item.children} children
                    {item.checkIn ? ` · ${item.rooms} rooms` : ""}
                  </p>
                </div>
                <Separator />
                <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
                  {q ? (
                    <>
                      <Status
                        value={q.available ? "Available" : "Unavailable"}
                      />
                      <span className="font-semibold">
                        {q.available
                          ? money(q.totalPrice, q.currency)
                          : "Not available"}
                      </span>
                    </>
                  ) : (
                    <span className="text-sm text-muted-foreground">
                      A live quote is needed before checkout.
                    </span>
                  )}
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>
      <Card className="shadow-none lg:sticky lg:top-24">
        <CardContent className="space-y-5">
          <h2>{checkout ? "Booking summary" : "Your trip summary"}</h2>
          <p className="text-sm text-muted-foreground">
            {cart.items.length} selection{cart.items.length === 1 ? "" : "s"} ·
            Prices supplied live
          </p>
          {quoteError && <ErrorState message={quoteError} />}
          <Separator />
          <div className="flex items-center justify-between gap-3">
            <span>Total</span>
            <span className="text-xl font-semibold">
              {canCheckout(quote)
                ? money(quote!.totalPrice!, quote!.currency!)
                : "Unavailable"}
            </span>
          </div>
          {quote && (
            <p className="text-xs text-muted-foreground">
              Last checked {new Date(quote.quotedAt).toLocaleTimeString()}.
              Availability is checked again when you confirm.
            </p>
          )}
          {!checkout && (
            <Button
              className="w-full"
              variant="outline"
              onClick={result.reload}
            >
              Refresh live quote
            </Button>
          )}
          {!canCheckout(quote) && (
            <p role="status" className="text-sm text-destructive">
              All selections must be available in the same currency before you
              can book.
            </p>
          )}
          {checkout ? (
            quote && canCheckout(quote) && children?.(quote, cart)
          ) : (
            <Button
              className="w-full"
              disabled={!canCheckout(quote)}
              asChild={canCheckout(quote)}
            >
              {canCheckout(quote) ? (
                <Link to="/checkout">Proceed to checkout</Link>
              ) : (
                "Proceed to checkout"
              )}
            </Button>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

