import { useState } from "react";
import { Pencil } from "lucide-react";
import { useCart } from "@/contexts/CartContext";
import type { CartItem, Product } from "@/types";
import { BookingSelectionForm } from "@/components/BookingSelectionForm";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";

export function CartItemEditor({
  item,
  product,
  disabled,
  onSaved,
}: {
  item: CartItem;
  product: Product;
  disabled: boolean;
  onSaved: () => void;
}) {
  const cart = useCart();
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  return (
    <Dialog
      open={open}
      onOpenChange={(value) => {
        if (!saving) setOpen(value);
      }}
    >
      <DialogTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          disabled={disabled || !product.isActive}
          aria-label={`Edit ${product.name}`}
        >
          <Pencil className="size-4" /> Edit
        </Button>
      </DialogTrigger>
      <DialogContent
        className="max-h-[90dvh] overflow-y-auto"
        showCloseButton={!saving}
      >
        <DialogHeader>
          <DialogTitle>Edit {product.name}</DialogTitle>
          <DialogDescription>
            Update your selection. Live prices and availability will refresh
            after saving.
          </DialogDescription>
        </DialogHeader>
        {open && (
          <BookingSelectionForm
            product={product}
            initialSelection={item}
            onSave={(selection) => cart.update(item.id, selection)}
            onBusyChange={setSaving}
            onCancel={() => setOpen(false)}
            onSaved={() => {
              setOpen(false);
              onSaved();
            }}
          />
        )}
      </DialogContent>
    </Dialog>
  );
}
