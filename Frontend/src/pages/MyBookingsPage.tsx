import { useState, type FormEvent } from "react";
import { useNavigate, Link } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import { savedOrders } from "@/services/orderService";
import { PageHeading, Empty } from "@/components/shared";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
export default function MyBookingsPage() {
  const navigate = useNavigate(),
    [ids] = useState(savedOrders),
    [error, setError] = useState("");
  function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const id = String(
      new FormData(e.currentTarget).get("orderId") || "",
    ).trim();
    if (
      !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(
        id,
      )
    ) {
      setError("Enter a valid order reference.");
      return;
    }
    navigate(`/orders/${id}`);
  }
  return (
    <>
      <PageHeading eyebrow="Your reservations" title="Find your bookings.">
        Open a recent order from this browser, or look one up using its
        reference.
      </PageHeading>
      <Card className="mb-8 max-w-2xl shadow-none">
        <CardContent>
          <form className="space-y-3" onSubmit={submit}>
            <Label htmlFor="orderId">Order reference</Label>
            <div className="flex flex-col gap-3 sm:flex-row">
              <Input
                id="orderId"
                name="orderId"
                placeholder="Enter your order ID"
                required
                aria-describedby={error ? "lookup-error" : undefined}
              />
              <Button type="submit">
                Find order
                <ArrowRight className="size-4" />
              </Button>
            </div>
            {error && (
              <p
                id="lookup-error"
                role="alert"
                className="text-sm text-destructive"
              >
                {error}
              </p>
            )}
          </form>
        </CardContent>
      </Card>
      <h2 className="mb-4">Recent orders on this browser</h2>
      {ids.length ? (
        <div className="space-y-3">
          {ids.map((id) => (
            <Card className="py-4 shadow-none" key={id}>
              <CardContent className="flex flex-wrap items-center justify-between gap-3">
                <span className="break-all font-mono text-sm">{id}</span>
                <Button asChild variant="outline" size="sm">
                  <Link to={`/orders/${id}`}>
                    View order
                    <ArrowRight className="size-4" />
                  </Link>
                </Button>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Empty title="No saved orders yet">
          Orders created or opened here will appear on this browser. You can
          always use an order reference above.
        </Empty>
      )}
    </>
  );
}
