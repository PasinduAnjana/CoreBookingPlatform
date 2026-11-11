import { useLoad } from "@/hooks/useLoad";
import { useState, useRef, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { MapPin, ArrowLeft, ShoppingBag, Check } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { products } from "@/services/productService";
import { useCart } from "@/contexts/CartContext";
import { attributes, type Product } from "@/types";
import { errorMessage } from "@/lib/api";
import { today } from "@/lib/utils";
import { Loading, ErrorState, ProductImage } from "@/components/shared";
function BookingForm({ product }: { product: Product }) {
  const attrs = attributes(product.attributesJson),
    hotel = product.supplierCode === "ABC",
    supported = hotel || product.supplierCode === "CDE";
  const [optionId, setOption] = useState(""),
    [slot, setSlot] = useState("");
  const [busy, setBusy] = useState(false),
    [added, setAdded] = useState(false),
    [error, setError] = useState("");
  const lock = useRef(false);
  const cart = useCart();
  const option = attrs.options.find((o) => o.supplierOptionId === optionId);
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (lock.current) return;
    setError("");
    setAdded(false);
    const form = new FormData(e.currentTarget),
      adults = Number(form.get("adults")),
      children = Number(form.get("children")),
      rooms = hotel ? Number(form.get("rooms")) : 1;
    const date = String(form.get("date") || ""),
      checkIn = String(form.get("checkIn") || ""),
      checkOut = String(form.get("checkOut") || "");
    let message = "";
    if (!option) message = "Choose a booking option.";
    else if (
      ![adults, children, rooms].every(Number.isSafeInteger) ||
      adults < 0 ||
      children < 0 ||
      adults + children < 1 ||
      rooms < 1
    )
      message = "Enter valid guest counts.";
    else if (hotel && (checkIn < today() || checkOut <= checkIn))
      message =
        "Check-out must be after check-in, and dates cannot be in the past.";
    else if (
      hotel &&
      (adults < rooms ||
        (option.maxAdults != null && adults > option.maxAdults * rooms) ||
        (option.maxChildren != null && children > option.maxChildren * rooms))
    )
      message =
        "The selected guests exceed this room’s occupancy limits, or there is less than one adult per room.";
    else if (!hotel && (date < today() || !attrs.timeSlots.includes(slot)))
      message = "Select a valid activity date and time slot.";
    else if (
      !hotel &&
      ((option.maxParticipants != null &&
        adults + children > option.maxParticipants) ||
        (adults > 0 && !option.passengerTypes?.includes("Adult")) ||
        (children > 0 && !option.passengerTypes?.includes("Child")))
    )
      message = "The package does not support this participant selection.";
    if (message) {
      setError(message);
      return;
    }
    lock.current = true;
    setBusy(true);
    try {
      await cart.add({
        productId: product.id,
        supplierOptionId: optionId,
        adults,
        children,
        rooms,
        ...(hotel
          ? { checkIn, checkOut }
          : { activityDate: date, timeSlot: slot }),
      });
      setAdded(true);
      toast.success("Added to your cart");
    } catch (err) {
      setError(errorMessage(err));
      toast.error("Could not add this selection");
    } finally {
      setBusy(false);
      lock.current = false;
    }
  }
  const unavailable =
    !supported || !attrs.options.length || (!hotel && !attrs.timeSlots.length);
  return (
    <Card className="shadow-none lg:sticky lg:top-24">
      <CardHeader>
        <CardTitle>Plan your {hotel ? "stay" : "experience"}</CardTitle>
        <p className="text-sm text-muted-foreground">
          Choose your options. Live prices are checked in your cart.
        </p>
      </CardHeader>
      <CardContent>
        <form onSubmit={submit} className="space-y-5">
          {unavailable && (
            <ErrorState
              message={
                !supported
                  ? "This supplier is not supported for booking."
                  : !attrs.options.length
                    ? "Booking options are unavailable."
                    : "Activity time slots are missing. Reimport this supplier’s catalog before booking."
              }
            />
          )}
          <div className="space-y-2">
            <Label htmlFor="option">
              {hotel ? "Room type" : "Activity package"}
            </Label>
            <Select
              value={optionId}
              onValueChange={(v) => {
                setOption(v);
                setAdded(false);
              }}
              disabled={busy || unavailable}
            >
              <SelectTrigger id="option" className="w-full">
                <SelectValue placeholder="Select an option" />
              </SelectTrigger>
              <SelectContent>
                {attrs.options.map((o) => (
                  <SelectItem
                    key={o.supplierOptionId}
                    value={o.supplierOptionId}
                  >
                    {o.name || o.label || o.supplierOptionId}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          {option && (
            <p className="text-xs leading-relaxed text-muted-foreground">
              {hotel
                ? `${option.bedType || "Room"} · Up to ${option.maxAdults ?? "—"} adults and ${option.maxChildren ?? "—"} children per room`
                : `Up to ${option.maxParticipants ?? "—"} participants · ${option.passengerTypes?.join(", ") || "See supplier requirements"}`}
            </p>
          )}
          {hotel ? (
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label htmlFor="checkIn">Check-in</Label>
                <Input
                  id="checkIn"
                  name="checkIn"
                  type="date"
                  min={today()}
                  required
                  disabled={busy}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="checkOut">Check-out</Label>
                <Input
                  id="checkOut"
                  name="checkOut"
                  type="date"
                  min={today()}
                  required
                  disabled={busy}
                />
              </div>
            </div>
          ) : (
            <>
              <div className="space-y-2">
                <Label htmlFor="date">Activity date</Label>
                <Input
                  id="date"
                  name="date"
                  type="date"
                  min={today()}
                  required
                  disabled={busy}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="slot">Time slot</Label>
                <Select
                  value={slot}
                  onValueChange={setSlot}
                  disabled={busy || unavailable}
                >
                  <SelectTrigger id="slot" className="w-full">
                    <SelectValue placeholder="Select a time" />
                  </SelectTrigger>
                  <SelectContent>
                    {attrs.timeSlots.map((s) => (
                      <SelectItem value={s} key={s}>
                        {s}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </>
          )}
          <div
            className={`grid gap-3 ${hotel ? "grid-cols-3" : "grid-cols-2"}`}
          >
            {[
              ["adults", "Adults", hotel ? 1 : 0, 1],
              ["children", "Children", 0, 0],
              ...(hotel ? [["rooms", "Rooms", 1, 1]] : []),
            ].map(([key, label, min, value]) => (
              <div key={key} className="space-y-2">
                <Label htmlFor={String(key)}>{label}</Label>
                <Input
                  id={String(key)}
                  name={String(key)}
                  type="number"
                  min={Number(min)}
                  step="1"
                  defaultValue={value}
                  required
                  disabled={busy}
                />
              </div>
            ))}
          </div>
          {error && <ErrorState message={error} />}
          <Separator />
          <Button
            className="w-full"
            disabled={busy || unavailable}
            type="submit"
          >
            <ShoppingBag className="size-4" />
            {busy ? "Adding…" : "Add to cart"}
          </Button>
          {added && (
            <div role="status" className="space-y-3 text-sm text-primary">
              <p className="flex items-center gap-2">
                <Check className="size-4" />
                Your selection is in the cart.
              </p>
              <Button variant="outline" asChild className="w-full">
                <Link to="/cart">Open cart</Link>
              </Button>
            </div>
          )}
        </form>
      </CardContent>
    </Card>
  );
}
export default function ProductDetailsPage() {
  const { id } = useParams(),
    numericId = Number(id);
  const result = useLoad(async () => {
    if (!Number.isInteger(numericId) || numericId < 1)
      throw new Error("Invalid product");
    return products.get(numericId);
  }, [id]);
  const content = useLoad(() => products.content(numericId), [id]);
  const p = result.data,
    c = content.data;
  return (
    <>
      <Button variant="ghost" asChild className="mb-6 -ml-3">
        <Link to="/">
          <ArrowLeft className="size-4" />
          All products
        </Link>
      </Button>
      {result.loading ? (
        <Loading />
      ) : result.error ? (
        <ErrorState message={result.error} retry={result.reload} />
      ) : (
        p && (
          <div className="grid items-start gap-8 lg:grid-cols-[1.45fr_1fr]">
            <div>
              <Badge variant="secondary" className="mb-4">
                {p.supplierCode === "ABC" ? "Hotel" : "Activity"}
              </Badge>
              <h1>{p.name}</h1>
              <p className="my-4 flex items-center gap-2 text-muted-foreground">
                <MapPin className="size-4" />
                {p.location || "Location not provided"}
              </p>
              <ProductImage
                name={p.name}
                content={c}
                className="mb-7 aspect-[16/10] rounded-xl"
              />
              {content.error && (
                <ErrorState
                  message="Product content could not be loaded."
                  retry={content.reload}
                />
              )}
              <section className="space-y-5">
                <h2>
                  About this {p.supplierCode === "ABC" ? "stay" : "experience"}
                </h2>
                <p className="whitespace-pre-line leading-relaxed text-muted-foreground">
                  {c?.description || "No description is available."}
                </p>
                {(
                  [
                    ["Highlights", c?.highlights],
                    ["Facilities", c?.facilities],
                    ["Included", c?.inclusions],
                    ["Not included", c?.exclusions],
                  ] as const
                ).map(([title, items]) =>
                  items?.length ? (
                    <div key={title}>
                      <h3 className="mb-3 font-semibold">{title}</h3>
                      <ul className="grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
                        {items.map((v, i) => (
                          <li className="flex gap-2" key={i}>
                            <Check className="mt-0.5 size-4 shrink-0 text-primary" />
                            {v}
                          </li>
                        ))}
                      </ul>
                    </div>
                  ) : null,
                )}
              </section>
              {c && c.images.length > 1 && (
                <div className="mt-7 grid grid-cols-2 gap-3">
                  {c.images.slice(1).map((im, i) => (
                    <ProductImage
                      key={i}
                      name={im.caption || p.name}
                      content={{ ...c, images: [im] }}
                      className="aspect-[4/3] rounded-lg"
                    />
                  ))}
                </div>
              )}
            </div>
            {p.isActive ? (
              <BookingForm key={p.id} product={p} />
            ) : (
              <ErrorState message="This product is no longer available for booking." />
            )}
          </div>
        )
      )}
    </>
  );
}
