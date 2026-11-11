import { Link } from "react-router-dom";
import { useLoad } from "@/hooks/useLoad";
import { products } from "@/services/productService";
import { SupplierSync } from "@/components/SupplierSync";
import { PageHeading, Loading, ErrorState, Empty } from "@/components/shared";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Table,
  TableHeader,
  TableHead,
  TableRow,
  TableBody,
  TableCell,
  TableCaption,
} from "@/components/ui/table";
export default function ManagedProductsPage() {
  const result = useLoad(products.list);
  return (
    <>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <PageHeading eyebrow="Supplier catalog" title="Imported products">
          Review supplier products, assign categories and maintain local
          content.
        </PageHeading>
        <SupplierSync onDone={result.reload} />
      </div>
      {result.loading ? (
        <Loading />
      ) : result.error ? (
        <ErrorState message={result.error} retry={result.reload} />
      ) : !result.data?.length ? (
        <Empty title="No imported products">
          Use Sync supplier catalog to fetch products from ABC and CDE.
        </Empty>
      ) : (
        <div className="overflow-hidden rounded-xl border bg-white">
          <Table>
            <TableCaption className="pb-4">
              All {result.data.length} imported products, including inactive
              entries
            </TableCaption>
            <TableHeader>
              <TableRow>
                <TableHead>Product</TableHead>
                <TableHead>Supplier reference</TableHead>
                <TableHead>Category</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Last updated</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {result.data.map((p) => (
                <TableRow key={p.id}>
                  <TableCell>
                    <Link
                      className="font-medium hover:text-primary"
                      to={`/management/products/${p.id}`}
                    >
                      {p.name}
                    </Link>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {p.location || "No location"}
                    </p>
                  </TableCell>
                  <TableCell>
                    <Badge variant="secondary">{p.supplierCode}</Badge>
                    <p className="mt-1 font-mono text-xs">
                      {p.supplierProductId}
                    </p>
                  </TableCell>
                  <TableCell>{p.category?.name || "Unassigned"}</TableCell>
                  <TableCell>
                    <Badge variant={p.isActive ? "default" : "secondary"}>
                      {p.isActive ? "Active" : "Inactive"}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground">
                    {p.lastUpdated
                      ? new Date(p.lastUpdated).toLocaleString()
                      : "Not recorded"}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button asChild variant="outline" size="sm">
                      <Link to={`/management/products/${p.id}`}>Manage</Link>
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </>
  );
}
