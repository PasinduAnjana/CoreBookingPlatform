import { useRef, useState, type FormEvent } from "react";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import { management, type Category } from "@/services/managementService";
import { useLoad } from "@/hooks/useLoad";
import { ErrorState, Empty, Loading, PageHeading } from "@/components/shared";
import { ConfirmDelete } from "@/components/ConfirmDelete";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Table,
  TableHeader,
  TableHead,
  TableRow,
  TableBody,
  TableCell,
  TableCaption,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { errorMessage } from "@/lib/api";
function CategoryForm({
  category,
  onDone,
  onClose,
  setBusy,
}: {
  category: Category | null;
  onDone: () => void;
  onClose: () => void;
  setBusy: (v: boolean) => void;
}) {
  const [saving, setSaving] = useState(false),
    [error, setError] = useState(""),
    lock = useRef(false);
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (lock.current) return;
    const f = new FormData(e.currentTarget),
      name = String(f.get("name") || "").trim(),
      description = String(f.get("description") || "").trim();
    if (!name) {
      setError("Enter a category name.");
      return;
    }
    lock.current = true;
    setSaving(true);
    setBusy(true);
    setError("");
    try {
      await management.saveCategory({ name, description }, category?.id);
      toast.success(category ? "Category updated" : "Category created");
      onDone();
      onClose();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      lock.current = false;
      setSaving(false);
      setBusy(false);
    }
  }
  return (
    <form onSubmit={submit} className="space-y-5">
      <div className="space-y-2">
        <Label htmlFor="category-name">Name</Label>
        <Input
          id="category-name"
          name="name"
          defaultValue={category?.name || ""}
          maxLength={100}
          required
          disabled={saving}
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="category-description">Description</Label>
        <Textarea
          id="category-description"
          name="description"
          defaultValue={category?.description || ""}
          maxLength={500}
          rows={4}
          disabled={saving}
        />
      </div>
      {error && <ErrorState message={error} />}
      <DialogFooter>
        <Button
          type="button"
          variant="outline"
          onClick={onClose}
          disabled={saving}
        >
          Cancel
        </Button>
        <Button disabled={saving} type="submit">
          {saving ? "Saving…" : "Save category"}
        </Button>
      </DialogFooter>
    </form>
  );
}
export default function CategoriesPage() {
  const result = useLoad(management.categories),
    [open, setOpen] = useState(false),
    [editing, setEditing] = useState<Category | null>(null),
    [busy, setBusy] = useState(false);
  function edit(category: Category | null) {
    setEditing(category);
    setOpen(true);
  }
  return (
    <>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <PageHeading title="Categories" eyebrow="Organize your catalog">
          Create internal categories and assign them to imported products.
        </PageHeading>
        <Button onClick={() => edit(null)}>
          <Plus className="size-4" />
          New category
        </Button>
      </div>
      {result.loading ? (
        <Loading />
      ) : result.error ? (
        <ErrorState message={result.error} retry={result.reload} />
      ) : !result.data?.length ? (
        <Empty title="No categories yet">
          Create a category to organize your hotels and activities.
        </Empty>
      ) : (
        <div className="overflow-hidden rounded-xl border bg-white">
          <Table>
            <TableCaption className="pb-4">
              {result.data.length} internally managed categories
            </TableCaption>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Description</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {result.data.map((c) => (
                <TableRow key={c.id}>
                  <TableCell className="font-medium">{c.name}</TableCell>
                  <TableCell className="max-w-md whitespace-normal text-muted-foreground">
                    {c.description || "—"}
                  </TableCell>
                  <TableCell>
                    <div className="flex justify-end gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => edit(c)}
                        aria-label={`Edit ${c.name}`}
                      >
                        Edit
                      </Button>
                      <ConfirmDelete
                        label="Delete"
                        title={`Delete ${c.name}?`}
                        description="Products assigned to this category must be reassigned first. This removes the category from the catalog."
                        action={() => management.deleteCategory(c.id)}
                        onDone={result.reload}
                      />
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
      <Dialog
        open={open}
        onOpenChange={(v) => {
          if (!busy) setOpen(v);
        }}
      >
        <DialogContent
          onInteractOutside={(e) => {
            if (busy) e.preventDefault();
          }}
          showCloseButton={!busy}
        >
          <DialogHeader>
            <DialogTitle>
              {editing ? "Edit category" : "New category"}
            </DialogTitle>
            <DialogDescription>
              Categories are managed locally and can be assigned to any imported
              product.
            </DialogDescription>
          </DialogHeader>
          <CategoryForm
            key={editing?.id || "new"}
            category={editing}
            onClose={() => setOpen(false)}
            onDone={result.reload}
            setBusy={setBusy}
          />
        </DialogContent>
      </Dialog>
    </>
  );
}
