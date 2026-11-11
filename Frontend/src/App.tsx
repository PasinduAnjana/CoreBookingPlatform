import {
  BrowserRouter,
  Routes,
  Route,
  NavLink,
  Link,
  useLocation,
} from "react-router-dom";
import { Suspense, lazy, useEffect } from "react";
import { Compass, ShoppingBag, Ticket } from "lucide-react";
import { Toaster } from "sonner";
import { CartProvider, useCart } from "@/contexts/CartContext";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
const ManagementLayout = lazy(
  () => import("@/pages/management/ManagementLayout"),
);
const ManagedProductsPage = lazy(
  () => import("@/pages/management/ManagedProductsPage"),
);
const ManagedProductDetailsPage = lazy(
  () => import("@/pages/management/ManagedProductDetailsPage"),
);
const CategoriesPage = lazy(() => import("@/pages/management/CategoriesPage"));
const ManagedOrdersPage = lazy(
  () => import("@/pages/management/ManagedOrdersPage"),
);
const ProductsPage = lazy(() => import("@/pages/ProductsPage"));
const ProductDetailsPage = lazy(() => import("@/pages/ProductDetailsPage"));
const CartPage = lazy(() => import("@/pages/CartPage"));
const CheckoutPage = lazy(() => import("@/pages/CheckoutPage"));
const OrderDetailsPage = lazy(() => import("@/pages/OrderDetailsPage"));
const MyBookingsPage = lazy(() => import("@/pages/MyBookingsPage"));
import { Empty, Loading } from "@/components/shared";
function Layout() {
  const { cart } = useCart(),
    location = useLocation();
  useEffect(() => {
    window.scrollTo(0, 0);
    document.getElementById("main")?.focus();
  }, [location.pathname]);
  const navClass = ({ isActive }: { isActive: boolean }) =>
    `inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${isActive ? "bg-accent text-primary" : "text-muted-foreground hover:bg-secondary hover:text-foreground"}`;
  return (
    <div className="flex min-h-screen flex-col">
      <a className="sr-only focus:not-sr-only focus:p-3" href="#main">
        Skip to content
      </a>
      <header className="sticky top-0 z-20 border-b bg-white">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-5 py-4 sm:px-8">
          <Link
            to="/"
            className="flex items-center gap-2.5 text-lg font-semibold tracking-tight"
          >
            <span className="flex size-9 items-center justify-center rounded-xl bg-primary text-white">
              <Compass className="size-5" />
            </span>
            Core Booking
          </Link>
          <nav
            aria-label="Main navigation"
            className="flex flex-wrap items-center gap-1"
          >
            <NavLink end to="/" className={navClass}>
              Products
            </NavLink>
            <NavLink to="/cart" className={navClass}>
              <ShoppingBag className="size-4" />
              Cart
              <Badge
                className="min-w-5 justify-center px-1.5"
                variant="secondary"
              >
                {cart?.items.length || 0}
              </Badge>
            </NavLink>
            <NavLink to="/bookings" className={navClass}>
              <Ticket className="hidden size-4 sm:block" />
              My bookings
            </NavLink>
            <NavLink to="/management" className={navClass}>
              Management
            </NavLink>
          </nav>
        </div>
      </header>
      <main
        id="main"
        tabIndex={-1}
        className="mx-auto w-full max-w-6xl flex-1 px-5 py-9 outline-none sm:px-8 sm:py-12"
      >
        <Suspense fallback={<Loading />}>
          <Routes>
            <Route path="/management" element={<ManagementLayout />}>
              <Route index element={<ManagedProductsPage />} />
              <Route
                path="products/:id"
                element={<ManagedProductDetailsPage />}
              />
              <Route path="categories" element={<CategoriesPage />} />
              <Route path="orders" element={<ManagedOrdersPage />} />
              <Route
                path="orders/:orderId"
                element={<OrderDetailsPage management />}
              />
            </Route>
            <Route path="/" element={<ProductsPage />} />
            <Route path="/products/:id" element={<ProductDetailsPage />} />
            <Route path="/cart" element={<CartPage />} />
            <Route path="/checkout" element={<CheckoutPage />} />
            <Route path="/orders/:orderId" element={<OrderDetailsPage />} />
            <Route path="/bookings" element={<MyBookingsPage />} />
            <Route
              path="*"
              element={
                <Empty title="Page not found">
                  <Button asChild className="mt-4">
                    <Link to="/">Explore products</Link>
                  </Button>
                </Empty>
              }
            />
          </Routes>
        </Suspense>
      </main>
      <footer className="mt-10 border-t bg-white">
        <div className="mx-auto flex max-w-6xl flex-wrap justify-between gap-2 px-5 py-6 text-xs text-muted-foreground sm:px-8">
          <span className="font-semibold">Core Booking</span>
          <span>Stays and experiences, together.</span>
        </div>
      </footer>
      <Toaster richColors position="bottom-right" />
    </div>
  );
}
export default function App() {
  return (
    <BrowserRouter>
      <CartProvider>
        <Layout />
      </CartProvider>
    </BrowserRouter>
  );
}
