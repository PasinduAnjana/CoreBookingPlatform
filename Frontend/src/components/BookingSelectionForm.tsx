import { useState, useRef, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { ShoppingBag, Check, CalendarIcon, Save } from "lucide-react";
import { format, parseISO, startOfDay } from "date-fns";
import type { DateRange } from "react-day-picker";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Calendar } from "@/components/ui/calendar";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { attributes, type Product, type Selection } from "@/types";
import { errorMessage } from "@/lib/api";
import { today } from "@/lib/utils";
import { ErrorState } from "@/components/shared";

export function BookingSelectionForm({
  product,
  initialSelection,
  onSave,
  onSaved,
  onBusyChange,
  onCancel,
}: {
  product: Product;
  initialSelection?: Selection;
  onSave: (selection: Selection) => Promise<void>;
  onSaved?: () => void;
  onBusyChange?: (busy: boolean) => void;
  onCancel?: () => void;
}) {
  const attrs = attributes(product.attributesJson),
    hotel = product.supplierCode === "ABC",
    supported = hotel || product.supplierCode === "CDE";
  const [optionId, setOption] = useState(
      initialSelection?.supplierOptionId ?? "",
    ),
    [slot, setSlot] = useState(initialSelection?.timeSlot ?? "");
  const [stay, setStay] = useState<DateRange | undefined>(() =>
    initialSelection?.checkIn
      ? {
          from: parseISO(initialSelection.checkIn),
          to: initialSelection.checkOut
            ? parseISO(initialSelection.checkOut)
            : undefined,
        }
      : undefined,
  );
  const [activityDate, setActivityDate] = useState<Date | undefined>(() =>
    initialSelection?.activityDate
      ? parseISO(initialSelection.activityDate)
      : undefined,
  );
  const [datesOpen, setDatesOpen] = useState(false);
  const [minimumDate, setMinimumDate] = useState(() => startOfDay(new Date()));
  const [busy, setBusy] = useState(false),
    [added, setAdded] = useState(false),
    [error, setError] = useState("");
  const lock = useRef(false);
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
    // Format local calendar dates directly, avoiding UTC date shifts.
    const date = activityDate ? format(activityDate, "yyyy-MM-dd") : "",
      checkIn = stay?.from ? format(stay.from, "yyyy-MM-dd") : "",
      checkOut = stay?.to ? format(stay.to, "yyyy-MM-dd") : "";
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
    else if (
      hotel &&
      (!checkIn || !checkOut || checkIn < today() || checkOut <= checkIn)
    )
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
    else if (
      !hotel &&
      (!date || date < today() || !attrs.timeSlots.includes(slot))
    )
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
    onBusyChange?.(true);
    try {
      await onSave({
        productId: product.id,
        supplierOptionId: optionId,
        adults,
        children,
        rooms,
        ...(hotel
          ? { checkIn, checkOut }
          : { activityDate: date, timeSlot: slot }),
      });
      setAdded(!initialSelection);
      toast.success(
        initialSelection ? "Cart item updated" : "Added to your cart",
      );
      onSaved?.();
    } catch (err) {
      setError(errorMessage(err));
      toast.error(
        initialSelection
          ? "Could not update this selection"
          : "Could not add this selection",
      );
    } finally {
      setBusy(false);
      onBusyChange?.(false);
      lock.current = false;
    }
  }
  const unavailable =
    !product.isActive ||
    !supported ||
    !attrs.options.length ||
    (!hotel && !attrs.timeSlots.length);
  return (
    <form onSubmit={submit} className="space-y-5">
      {unavailable && (
        <ErrorState
          message={
            !product.isActive
              ? "This product is no longer available for booking."
              : !supported
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
              <SelectItem key={o.supplierOptionId} value={o.supplierOptionId}>
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
      <div className="space-y-2">
        <Label htmlFor="booking-dates">
          {hotel ? "Check-in / check-out" : "Activity date"}
        </Label>
        <Popover
          open={datesOpen}
          onOpenChange={(open) => {
            if (open) setMinimumDate(startOfDay(new Date()));
            setDatesOpen(open);
          }}
        >
          <PopoverTrigger asChild>
            <Button
              id="booking-dates"
              type="button"
              variant="outline"
              className="w-full justify-start text-left font-normal"
              aria-describedby="booking-dates-hint"
              disabled={busy || unavailable}
            >
              <CalendarIcon className="size-4 shrink-0" />
              <span className="truncate">
                {hotel
                  ? stay?.from
                    ? `${format(stay.from, "MMM d, yyyy")} → ${stay.to ? format(stay.to, "MMM d, yyyy") : "Check-out"}`
                    : "Select your stay dates"
                  : activityDate
                    ? format(activityDate, "MMM d, yyyy")
                    : "Select activity date"}
              </span>
            </Button>
          </PopoverTrigger>
          <PopoverContent
            align="end"
            className="w-auto max-w-[calc(100vw-2rem)] max-h-[var(--radix-popover-content-available-height)] overflow-y-auto p-0"
            aria-label={
              hotel
                ? "Select check-in and check-out dates"
                : "Select activity date"
            }
          >
            {hotel ? (
              <Calendar
                mode="range"
                numberOfMonths={2}
                selected={stay}
                defaultMonth={stay?.from}
                min={1}
                excludeDisabled
                disabled={busy || unavailable ? true : { before: minimumDate }}
                autoFocus
                classNames={{
                  months: "relative flex flex-col gap-4 sm:flex-row",
                }}
                onSelect={(value) => {
                  setStay(value);
                  setAdded(false);
                  setError("");
                }}
              />
            ) : (
              <Calendar
                mode="single"
                selected={activityDate}
                defaultMonth={activityDate}
                disabled={busy || unavailable ? true : { before: minimumDate }}
                autoFocus
                onSelect={(value) => {
                  setActivityDate(value);
                  setAdded(false);
                  setError("");
                  if (value) setDatesOpen(false);
                }}
              />
            )}
            <div className="flex items-center justify-between gap-3 border-t p-3">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={busy}
                onClick={() => {
                  setStay(undefined);
                  setActivityDate(undefined);
                  setAdded(false);
                  setError("");
                }}
              >
                Clear dates
              </Button>
              <Button
                type="button"
                size="sm"
                disabled={
                  busy || (hotel ? !stay?.from || !stay?.to : !activityDate)
                }
                onClick={() => setDatesOpen(false)}
              >
                Done
              </Button>
            </div>
          </PopoverContent>
        </Popover>
        <p id="booking-dates-hint" className="text-xs text-muted-foreground">
          {hotel
            ? "Select check-in first, then check-out. Minimum one night."
            : "Choose the date for your experience."}
        </p>
      </div>
      {!hotel && (
        <>
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
      <div className={`grid gap-3 ${hotel ? "grid-cols-3" : "grid-cols-2"}`}>
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
              defaultValue={
                initialSelection?.[key as "adults" | "children" | "rooms"] ??
                value
              }
              required
              disabled={busy}
            />
          </div>
        ))}
      </div>
      {error && <ErrorState message={error} />}
      <Separator />
      <div className="flex gap-3">
        {onCancel && (
          <Button
            type="button"
            variant="outline"
            disabled={busy}
            onClick={onCancel}
          >
            Cancel
          </Button>
        )}
        <Button className="flex-1" disabled={busy || unavailable} type="submit">
          {initialSelection ? (
            <Save className="size-4" />
          ) : (
            <ShoppingBag className="size-4" />
          )}
          {initialSelection
            ? busy
              ? "Saving…"
              : "Save changes"
            : busy
              ? "Adding…"
              : "Add to cart"}
        </Button>
      </div>
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
  );
}
