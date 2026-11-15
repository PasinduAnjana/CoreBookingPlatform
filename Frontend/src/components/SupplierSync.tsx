import { useRef, useState } from "react";
import { RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { management, type ImportSummary } from "@/services/managementService";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from "@/components/ui/select";
import {
  Dialog,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import {
  Table,
  TableHeader,
  TableHead,
  TableRow,
  TableBody,
  TableCell,
} from "@/components/ui/table";
import { ErrorState } from "@/components/shared";
import { errorMessage, isNotFound } from "@/lib/api";
import { useLoad } from "@/hooks/useLoad";
export function SupplierSync({ onDone }: { onDone: () => void }) {
  const [open, setOpen] = useState(false),
    [supplier, setSupplier] = useState("all"),
    [busy, setBusy] = useState(false),
    [results, setResults] = useState<ImportSummary[]>([]),
    [errors, setErrors] = useState<string[]>([]),
    lock = useRef(false);
  const suppliers = useLoad(
    () => (open ? management.suppliers() : Promise.resolve([])),
    [open],
  );
  async function sync() {
    if (lock.current) return;
    lock.current = true;
    setBusy(true);
    setResults([]);
    setErrors([]);
    let failed = false;
    try {
      const selected = (suppliers.data ?? []).filter(
        (s) => supplier === "all" || s.code === supplier,
      );
      if (!selected.length) {
        setErrors(["Select an available supplier."]);
        return;
      }
      for (const { code } of selected) {
        try {
          const r = await management.sync(code);
          setResults((v) => [...v, r]);
          if (r.failed > 0) failed = true;
        } catch (e) {
          failed = true;
          setErrors((v) => [
            ...v,
            `${code}: ${isNotFound(e) ? "Catalog import is only available when the backend runs in Development." : errorMessage(e)}`,
          ]);
        }
      }
      onDone();
      if (failed)
        toast.warning("Import completed with errors. Review the results.");
      else toast.success("Supplier catalog refreshed");
    } finally {
      lock.current = false;
      setBusy(false);
    }
  }
  return (
    <Dialog
      open={open}
      onOpenChange={(v) => {
        if (!busy) setOpen(v);
      }}
    >
      <DialogTrigger asChild>
        <Button>
          <RefreshCw className="size-4" />
          Sync supplier catalog
        </Button>
      </DialogTrigger>
      <DialogContent
        className="max-h-[85vh] overflow-y-auto sm:max-w-xl"
        showCloseButton={!busy}
        onInteractOutside={(e) => {
          if (busy) e.preventDefault();
        }}
      >
        <DialogHeader>
          <DialogTitle>Sync supplier catalog</DialogTitle>
          <DialogDescription>
            Import new products and refresh existing supplier fields and
            content. Reimporting overwrites locally edited descriptions, images
            and content lists. Categories and active states are preserved.
            Available in Development only.
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-2">
          <Label htmlFor="supplier">Supplier</Label>
          <Select value={supplier} onValueChange={setSupplier} disabled={busy}>
            <SelectTrigger id="supplier" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All suppliers</SelectItem>
              {suppliers.data?.map((s) => (
                <SelectItem key={s.code} value={s.code}>
                  {s.name} · {s.code}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        {suppliers.error && (
          <ErrorState message={suppliers.error} retry={suppliers.reload} />
        )}
        {!suppliers.loading && !suppliers.error && !suppliers.data?.length && (
          <p>No supplier adapters are registered.</p>
        )}
        <Button
          disabled={
            busy ||
            suppliers.loading ||
            !!suppliers.error ||
            !suppliers.data?.length
          }
          onClick={() => void sync()}
        >
          <RefreshCw className={`size-4 ${busy ? "animate-spin" : ""}`} />
          {busy ? "Importing products…" : "Run import"}
        </Button>
        <div aria-live="polite">
          {results.length > 0 && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Supplier</TableHead>
                  <TableHead>New</TableHead>
                  <TableHead>Updated</TableHead>
                  <TableHead>Failed</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {results.map((r) => (
                  <TableRow key={r.supplierCode}>
                    <TableCell>{r.supplierCode}</TableCell>
                    <TableCell>{r.created}</TableCell>
                    <TableCell>{r.updated}</TableCell>
                    <TableCell>{r.failed}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
          {results
            .filter((r) => r.failed > 0)
            .map((r) => (
              <p className="mt-3 text-sm text-destructive" key={r.supplierCode}>
                {r.failed} of {r.total} {r.supplierCode} products could not be
                imported. Retry when the supplier and product services are
                available.
              </p>
            ))}
        </div>
        {errors.map((e, i) => (
          <ErrorState key={i} message={e} />
        ))}
      </DialogContent>
    </Dialog>
  );
}
