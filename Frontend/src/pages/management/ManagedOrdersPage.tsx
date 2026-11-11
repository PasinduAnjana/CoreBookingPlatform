import { useState } from "react";
import { Link } from "react-router-dom";
import { useLoad } from "@/hooks/useLoad";
import { management } from "@/services/managementService";
import {
  PageHeading,
  Loading,
  ErrorState,
  Empty,
  Status,
} from "@/components/shared";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableHeader,
  TableHead,
  TableRow,
  TableBody,
  TableCell,
  TableCaption,
} from "@/components/ui/table";
import { money } from "@/lib/utils";
export default function ManagedOrdersPage() {
  const [page, setPage] = useState(1),
    result = useLoad(() => management.orders(page), [page]),
    data = result.data;
  return (
    <>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <PageHeading eyebrow="Orders & bookings" title="Orders">
          All persisted orders and their supplier booking outcomes, newest
          first.
        </PageHeading>
        <Button
          variant="outline"
          onClick={result.reload}
          disabled={result.loading}
        >
          Refresh orders
        </Button>
      </div>
      {result.loading ? (
        <Loading />
      ) : result.error ? (
        <ErrorState message={result.error} retry={result.reload} />
      ) : (
        data && (
          <>
            {!data.items.length ? (
              <Empty title="No orders on this page">
                Completed checkout attempts appear here, including failed and
                cancelled orders.
              </Empty>
            ) : (
              <div className="overflow-hidden rounded-xl border bg-white">
                <Table>
                  <TableCaption className="pb-4">
                    {data.totalCount} orders in the core database
                  </TableCaption>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Order / customer</TableHead>
                      <TableHead>Created</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Supplier bookings</TableHead>
                      <TableHead>Total</TableHead>
                      <TableHead className="text-right">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {data.items.map((o) => (
                      <TableRow key={o.id}>
                        <TableCell>
                          <Link
                            className="font-medium hover:text-primary"
                            to={`/management/orders/${o.id}`}
                          >
                            {o.customerName}
                          </Link>
                          <p className="mt-1 font-mono text-xs text-muted-foreground">
                            {o.id}
                          </p>
                        </TableCell>
                        <TableCell className="text-xs">
                          {new Date(o.createdAt).toLocaleString()}
                        </TableCell>
                        <TableCell>
                          <Status value={o.status} />
                        </TableCell>
                        <TableCell>
                          <p className="text-sm">
                            {o.bookings.length} bookings
                          </p>
                          <p className="mt-1 text-xs text-muted-foreground">
                            {[
                              ...new Set(o.bookings.map((b) => b.supplierCode)),
                            ].join(", ") || "—"}
                          </p>
                        </TableCell>
                        <TableCell>{money(o.totalPrice, o.currency)}</TableCell>
                        <TableCell className="text-right">
                          <Button size="sm" variant="outline" asChild>
                            <Link to={`/management/orders/${o.id}`}>
                              View order
                            </Link>
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
            <div className="mt-5 flex flex-wrap items-center justify-between gap-3">
              <p className="text-sm text-muted-foreground">
                Page {page} of{" "}
                {Math.max(1, Math.ceil(data.totalCount / data.pageSize))}
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  disabled={page <= 1}
                  onClick={() => setPage((v) => v - 1)}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  disabled={page * data.pageSize >= data.totalCount}
                  onClick={() => setPage((v) => v + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          </>
        )
      )}
    </>
  );
}
