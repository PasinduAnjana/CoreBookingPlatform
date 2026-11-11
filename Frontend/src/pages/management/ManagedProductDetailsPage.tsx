import { useRef, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { toast } from "sonner";
import { products } from "@/services/productService";
import { management, type Category } from "@/services/managementService";
import type { Product } from "@/types";
import { useLoad } from "@/hooks/useLoad";
import { PageHeading, Loading, ErrorState } from "@/components/shared";
import { ContentEditor } from "@/components/ContentEditor";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { errorMessage } from "@/lib/api";
function ProductSettings({
  product,
  categories,
}: {
  product: Product;
  categories: Category[];
}) {
  const [category, setCategory] = useState(
      product.categoryId?.toString() || "none",
    ),
    [active, setActive] = useState(product.isActive),
    [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    lock = useRef(false);
  async function save(e: FormEvent) {
    e.preventDefault();
    if (lock.current) return;
    lock.current = true;
    setBusy(true);
    setError("");
    try {
      await management.updateProduct(product.id, {
        categoryId: category === "none" ? null : Number(category),
        isActive: active,
      });
      toast.success("Product settings saved");
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      lock.current = false;
      setBusy(false);
    }
  }
  return (
    <Card className="shadow-none">
      <CardHeader>
        <CardTitle>Catalog settings</CardTitle>
      </CardHeader>
      <CardContent>
        <form className="space-y-5" onSubmit={save}>
          <div className="space-y-2">
            <Label htmlFor="product-category">Category</Label>
            <Select
              value={category}
              onValueChange={setCategory}
              disabled={busy}
            >
              <SelectTrigger id="product-category" className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="none">Unassigned</SelectItem>
                {categories.map((c) => (
                  <SelectItem key={c.id} value={String(c.id)}>
                    {c.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex items-center justify-between gap-4">
            <div>
              <Label htmlFor="product-active">Active product</Label>
              <p className="mt-1 text-xs text-muted-foreground">
                Inactive products are hidden from customer browsing and cannot
                be added to a cart.
              </p>
            </div>
            <Switch
              id="product-active"
              checked={active}
              onCheckedChange={setActive}
              disabled={busy}
            />
          </div>
          {error && <ErrorState message={error} />}
          <Button disabled={busy} type="submit">
            {busy ? "Saving…" : "Save settings"}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
function readable(json?: string) {
  try {
    return JSON.stringify(JSON.parse(json || "{}"), null, 2);
  } catch {
    return json || "No attributes stored";
  }
}
export default function ManagedProductDetailsPage() {
  const { id } = useParams(),
    productId = Number(id);
  const result = useLoad(async () => {
    if (!Number.isSafeInteger(productId) || productId < 1)
      throw new Error("Invalid product");
    return products.get(productId);
  }, [id]);
  const categories = useLoad(management.categories),
    content = useLoad(() => products.content(productId), [id]),
    p = result.data;
  return (
    <>
      <Button asChild variant="ghost" className="mb-5 -ml-3">
        <Link to="/management">← Imported products</Link>
      </Button>
      {result.loading ? (
        <Loading />
      ) : result.error ? (
        <ErrorState message={result.error} retry={result.reload} />
      ) : (
        p && (
          <>
            <div className="flex flex-wrap items-start justify-between gap-4">
              <PageHeading eyebrow="Product management" title={p.name}>
                {p.location || "Location not provided"}
              </PageHeading>
              <Button asChild variant="outline">
                <Link to={`/products/${p.id}`}>View booking page</Link>
              </Button>
            </div>
            <div className="grid items-start gap-6 lg:grid-cols-[1fr_2fr]">
              <div className="space-y-6">
                <Card className="shadow-none">
                  <CardHeader>
                    <CardTitle>Supplier details</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4 text-sm">
                    <Badge variant="secondary">{p.supplierCode}</Badge>
                    <dl className="space-y-3">
                      <div>
                        <dt className="text-muted-foreground">
                          Product reference
                        </dt>
                        <dd className="mt-1 break-all font-mono">
                          {p.supplierProductId}
                        </dd>
                      </div>
                      <div>
                        <dt className="text-muted-foreground">
                          Core product ID
                        </dt>
                        <dd className="mt-1">{p.id}</dd>
                      </div>
                      <div>
                        <dt className="text-muted-foreground">Last updated</dt>
                        <dd className="mt-1">
                          {p.lastUpdated
                            ? new Date(p.lastUpdated).toLocaleString()
                            : "Not recorded"}
                        </dd>
                      </div>
                    </dl>
                    <p className="text-xs leading-relaxed text-muted-foreground">
                      Name, location and booking options are maintained by the
                      supplier import.
                    </p>
                  </CardContent>
                </Card>
                {categories.loading ? (
                  <Loading />
                ) : categories.error ? (
                  <ErrorState
                    message={categories.error}
                    retry={categories.reload}
                  />
                ) : (
                  <ProductSettings
                    key={p.id}
                    product={p}
                    categories={categories.data || []}
                  />
                )}
                <Card className="shadow-none">
                  <CardHeader>
                    <CardTitle>Booking attributes</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <pre className="max-h-80 overflow-auto whitespace-pre-wrap break-all rounded-lg bg-muted p-3 text-xs">
                      {readable(p.attributesJson)}
                    </pre>
                  </CardContent>
                </Card>
              </div>
              {content.loading ? (
                <Loading />
              ) : content.error ? (
                <ErrorState message={content.error} retry={content.reload} />
              ) : (
                <ContentEditor
                  productId={p.id}
                  content={content.data}
                  onDone={content.reload}
                />
              )}
            </div>
          </>
        )
      )}
    </>
  );
}
