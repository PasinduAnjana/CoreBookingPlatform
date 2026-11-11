import { PageHeading } from "@/components/shared";
import { CartView } from "@/components/CartView";
export default function CartPage() {
  return (
    <>
      <PageHeading eyebrow="Your trip" title="Everything in one cart.">
        Review your selections and check live availability before booking.
      </PageHeading>
      <CartView />
    </>
  );
}
