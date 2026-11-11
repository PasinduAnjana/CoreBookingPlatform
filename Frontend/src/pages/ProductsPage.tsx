import { useLoad } from "@/hooks/useLoad";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { products } from "@/services/productService";
import {
  Loading,
  ErrorState,
  Empty,
  PageHeading,
  ProductCard,
} from "@/components/shared";
export default function ProductsPage() {
  const result = useLoad(products.list);
  const [filter, setFilter] = useState("all");
  const items =
    result.data?.filter(
      (p) => p.isActive && (filter === "all" || p.supplierCode === filter),
    ) || [];
  return (
    <>
      <PageHeading
        eyebrow="Stays & experiences"
        title="A little escape starts here."
      >
        Explore hotels and activities. Choose your dates and options to get a
        live price for your trip.
      </PageHeading>
      <div className="mb-7 flex flex-wrap gap-2" aria-label="Filter products">
        {[
          ["all", "All products"],
          ["ABC", "Hotels"],
          ["CDE", "Activities"],
        ].map(([value, label]) => (
          <Button
            key={value}
            aria-pressed={filter === value}
            variant={filter === value ? "default" : "outline"}
            onClick={() => setFilter(value)}
          >
            {label}
          </Button>
        ))}
      </div>
      {result.loading ? (
        <Loading />
      ) : result.error ? (
        <ErrorState message={result.error} retry={result.reload} />
      ) : items.length ? (
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((p) => (
            <ProductCard key={p.id} product={p} />
          ))}
        </div>
      ) : (
        <Empty title="No products available">
          There are no active products in this selection. Try another filter or
          check back after the catalog has been imported.
        </Empty>
      )}
    </>
  );
}
