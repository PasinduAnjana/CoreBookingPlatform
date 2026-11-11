import { NavLink, Outlet, useLocation } from "react-router-dom";
import { Package, FolderTree, ReceiptText } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
export default function ManagementLayout() {
  const { pathname } = useLocation();
  return (
    <>
      <div className="mb-7 flex flex-col justify-between gap-5 sm:flex-row sm:items-center">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[.18em] text-primary">
            Management
          </p>
          <p className="mt-2 text-sm text-muted-foreground">
            Catalog, content and supplier bookings.
          </p>
        </div>
        <nav
          aria-label="Management navigation"
          className="flex flex-wrap gap-2"
        >
          {[
            { to: "/management", label: "Products", icon: Package, end: true },
            {
              to: "/management/categories",
              label: "Categories",
              icon: FolderTree,
            },
            { to: "/management/orders", label: "Orders", icon: ReceiptText },
          ].map(({ to, label, icon: Icon, end }) => (
            <NavLink key={to} to={to} end={end}>
              {({ isActive }) => (
                <Button asChild variant={isActive || (end && pathname.startsWith("/management/products/")) ? "default" : "outline"}>
                  <span>
                    <Icon className="size-4" />
                    {label}
                  </span>
                </Button>
              )}
            </NavLink>
          ))}
        </nav>
      </div>
      <Separator className="mb-8" />
      <Outlet />
    </>
  );
}
