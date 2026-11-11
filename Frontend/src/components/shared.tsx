import { useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { AlertCircle, ArrowRight, ImageOff, MapPin } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent } from "@/components/ui/card";
import { products } from "@/services/productService";
import { useLoad } from "@/hooks/useLoad";
import type { Content, Product } from "@/types";
export function PageHeading({
  eyebrow,
  title,
  children,
}: {
  eyebrow?: string;
  title: string;
  children?: ReactNode;
}) {
  return (
    <div className="mb-8">
      <p className="mb-3 text-xs font-semibold uppercase tracking-[.18em] text-primary">
        {eyebrow || "Core Booking"}
      </p>
      <h1>{title}</h1>
      {children && (
        <div className="mt-3 max-w-2xl text-muted-foreground">{children}</div>
      )}
    </div>
  );
}
export function ErrorState({
  message,
  retry,
}: {
  message: string;
  retry?: () => void;
}) {
  return (
    <div
      role="alert"
      className="flex flex-wrap items-center gap-3 rounded-xl border border-destructive/20 bg-white p-5"
    >
      <AlertCircle className="size-5 shrink-0 text-destructive" />
      <p className="flex-1 text-sm">{message}</p>
      {retry && (
        <Button variant="outline" onClick={retry}>
          Try again
        </Button>
      )}
    </div>
  );
}
export function Loading() {
  return (
    <div
      aria-label="Loading"
      role="status"
      className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3"
    >
      {[1, 2, 3].map((i) => (
        <Skeleton key={i} className="h-64 rounded-xl" />
      ))}
      <span className="sr-only">Loading…</span>
    </div>
  );
}
export function Empty({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <div className="rounded-xl border border-dashed bg-white px-6 py-14 text-center">
      <h2>{title}</h2>
      <div className="mx-auto mt-3 max-w-md text-sm text-muted-foreground">
        {children}
      </div>
    </div>
  );
}
export function Status({ value }: { value: string }) {
  const style = /^(Confirmed|Cancelled)$/.test(value)
    ? "bg-teal-50 text-teal-800 border-teal-200"
    : /Failed|Unknown|Attention/.test(value)
      ? "bg-amber-50 text-amber-900 border-amber-200"
      : "bg-slate-100 text-slate-700";
  return (
    <Badge variant="outline" className={style}>
      {value.replace(/([a-z])([A-Z])/g, "$1 $2")}
    </Badge>
  );
}
export function ProductImage({
  content,
  name,
  className = "",
}: {
  content?: Content | null;
  name: string;
  className?: string;
}) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null);
  const url =
    content?.images?.find((i) => i.isPrimary)?.url || content?.images?.[0]?.url;

  return (
    <div className={`overflow-hidden bg-secondary ${className}`}>
      {url && failedUrl !== url ? (
        <img
          className="size-full object-cover"
          src={url}
          alt={name}
          loading="lazy"
          onError={() => setFailedUrl(url)}
        />
      ) : (
        <div className="flex size-full min-h-40 flex-col items-center justify-center gap-2 text-muted-foreground">
          <ImageOff className="size-7" />
          <span className="text-xs">No image available</span>
        </div>
      )}
    </div>
  );
}
export function ProductCard({ product }: { product: Product }) {
  const content = useLoad(() => products.content(product.id), [product.id]);
  return (
    <Card className="gap-0 overflow-hidden py-0 shadow-none transition-shadow hover:shadow-md">
      <ProductImage
        name={product.name}
        content={content.data}
        className="aspect-[16/10]"
      />
      <CardContent className="space-y-3 p-5">
        <Badge variant="secondary">
          {product.category?.name ||
            (product.supplierCode === "ABC"
              ? "Hotel"
              : product.supplierCode === "CDE"
                ? "Activity"
                : product.supplierCode)}
        </Badge>
        <h2 className="text-lg">{product.name}</h2>
        {product.location && (
          <p className="flex items-center gap-1.5 text-sm text-muted-foreground">
            <MapPin className="size-3.5" />
            {product.location}
          </p>
        )}
        {content.data?.description && (
          <p className="line-clamp-2 text-sm text-muted-foreground">
            {content.data.description}
          </p>
        )}
        {content.error && (
          <p className="text-xs text-muted-foreground">
            Additional content is temporarily unavailable.
          </p>
        )}
        <Button variant="outline" asChild className="mt-2 w-full">
          <Link to={`/products/${product.id}`}>
            View details
            <ArrowRight className="size-4" />
          </Link>
        </Button>
      </CardContent>
    </Card>
  );
}
