import { useLoad } from "@/hooks/useLoad";
import { Link, useParams } from "react-router-dom";
import { MapPin, ArrowLeft, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { products } from "@/services/productService";
import { useCart } from "@/contexts/CartContext";
import type { Product } from "@/types";
import { Loading, ErrorState, ProductImage } from "@/components/shared";
import { BookingSelectionForm } from "@/components/BookingSelectionForm";

function BookingForm({ product }: { product: Product }) {
  const cart = useCart();
  return (
    <Card className="shadow-none lg:sticky lg:top-24">
      <CardHeader>
        <CardTitle>
          Plan your {product.productType === "Hotel" ? "stay" : "experience"}
        </CardTitle>
        <p className="text-sm text-muted-foreground">
          Choose your options. Live prices are checked in your cart.
        </p>
      </CardHeader>
      <CardContent>
        <BookingSelectionForm product={product} onSave={cart.add} />
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
                {p.productType === "Hotel" ? "Hotel" : "Activity"}
              </Badge>
              <h1>{p.name}</h1>
              <p className="my-4 flex items-center gap-2 text-muted-foreground">
                <MapPin className="size-4" />
                {p.location || "Location not provided"}
              </p>
              <ProductImage
                name={p.name}
                content={c}
                priority
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
                  About this {p.productType === "Hotel" ? "stay" : "experience"}
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
