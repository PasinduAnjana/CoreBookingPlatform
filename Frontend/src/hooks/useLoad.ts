import { useEffect, useState } from "react";
import { errorMessage } from "@/lib/api";
export function useLoad<T>(load: () => Promise<T>, keys: unknown[] = []) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError("");
    setData(null);
    load()
      .then((v) => {
        if (active) setData(v);
      })
      .catch((e) => {
        if (active) setError(errorMessage(e));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
    // Each caller supplies the stable request keys.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...keys, revision]);
  return { data, error, loading, reload: () => setRevision((v) => v + 1) };
}
