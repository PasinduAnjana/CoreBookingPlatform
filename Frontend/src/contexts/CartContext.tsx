import {
  createContext,
  useContext,
  useState,
  useEffect,
  useRef,
  type ReactNode,
} from "react";
import { carts } from "@/services/cartService";
import { isNotFound } from "@/lib/api";
import type { Cart, Selection } from "@/types";
const KEY = "core-booking.cart";
function storedId() {
  try {
    return localStorage.getItem(KEY);
  } catch {
    return null;
  }
}
function persist(id: string | null) {
  try {
    if (id) localStorage.setItem(KEY, id);
    else localStorage.removeItem(KEY);
  } catch {
    /* Keep the in-memory cart available. */
  }
}
interface CartState {
  cartId: string | null;
  cart: Cart | null;
  add: (item: Selection) => Promise<void>;
  refresh: () => Promise<Cart | null>;
  release: () => void;
  remove: (id: number) => Promise<void>;
}
const Context = createContext<CartState | null>(null);
export function CartProvider({ children }: { children: ReactNode }) {
  const [cartId, setId] = useState(storedId);
  const [cart, setCart] = useState<Cart | null>(null);
  const idRef = useRef(cartId);
  const creating = useRef<Promise<Cart> | null>(null);
  function release() {
    idRef.current = null;
    persist(null);
    setId(null);
    setCart(null);
  }
  async function refresh() {
    const id = idRef.current;
    if (!id) return null;
    try {
      const data = await carts.get(id);
      if (idRef.current === id) setCart(data);
      return data;
    } catch (e) {
      if (isNotFound(e) && idRef.current === id) release();
      throw e;
    }
  }
  useEffect(() => {
    void refresh().catch(() => {});
    function sync() {
      const id = storedId();
      idRef.current = id;
      setId(id);
      setCart(null);
      void refresh().catch(() => {});
    }
    window.addEventListener("storage", sync);
    return () => window.removeEventListener("storage", sync);
  }, []);
  async function add(item: Selection) {
    if (!idRef.current) {
      creating.current ??= carts.create();
      try {
        const c = await creating.current;
        idRef.current = c.id;
        persist(c.id);
        setId(c.id);
      } finally {
        creating.current = null;
      }
    }
    setCart(await carts.add(idRef.current!, item));
  }
  async function remove(id: number) {
    if (idRef.current) {
      await carts.remove(idRef.current, id);
      await refresh();
    }
  }
  return (
    <Context.Provider value={{ cartId, cart, add, refresh, release, remove }}>
      {children}
    </Context.Provider>
  );
}
// oxlint-disable-next-line react/only-export-components -- Keep this small provider and its hook together.
export function useCart() {
  const value = useContext(Context);
  if (!value) throw new Error("CartProvider missing");
  return value;
}

