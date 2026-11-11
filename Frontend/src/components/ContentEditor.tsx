import { useRef, useState, type FormEvent } from "react";
import { Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { management, type ContentInput } from "@/services/managementService";
import type { Content } from "@/types";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Separator } from "@/components/ui/separator";
import { ErrorState } from "@/components/shared";
import { ConfirmDelete } from "@/components/ConfirmDelete";
import { errorMessage } from "@/lib/api";
function lines(value: FormDataEntryValue | null) {
  return String(value || "")
    .split(/\r?\n/)
    .map((s) => s.trim())
    .filter(Boolean);
}
export function ContentEditor({
  productId,
  content,
  onDone,
}: {
  productId: number;
  content: Content | null;
  onDone: () => void;
}) {
  const [images, setImages] = useState(content?.images || []),
    [error, setError] = useState(""),
    [busy, setBusy] = useState(false),
    lock = useRef(false);
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (lock.current) return;
    const f = new FormData(e.currentTarget),
      extra = String(f.get("additionalInfoJson") || "").trim();
    setError("");
    if (extra) {
      try {
        JSON.parse(extra);
      } catch {
        setError(
          "Additional information must contain valid JSON, or be left empty.",
        );
        return;
      }
    }
    const value: ContentInput = {
      description: String(f.get("description") || ""),
      images,
      highlights: lines(f.get("highlights")),
      facilities: lines(f.get("facilities")),
      inclusions: lines(f.get("inclusions")),
      exclusions: lines(f.get("exclusions")),
      additionalInfoJson: extra || null,
    };
    lock.current = true;
    setBusy(true);
    try {
      await management.saveContent(productId, value);
      toast.success("Product content saved");
      onDone();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      lock.current = false;
      setBusy(false);
    }
  }
  function updateImage(i: number, patch: Partial<Content["images"][number]>) {
    setImages((v) =>
      v.map((im, index) => (index === i ? { ...im, ...patch } : im)),
    );
  }
  return (
    <Card className="shadow-none">
      <CardHeader>
        <CardTitle>Product content</CardTitle>
        <CardDescription>
          {content
            ? "Edit locally stored content. A supplier reimport may overwrite these fields."
            : "No stored content exists. Add content for this imported product."}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={submit} className="space-y-6">
          <fieldset className="space-y-6" disabled={busy}>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                name="description"
                rows={5}
                defaultValue={content?.description || ""}
              />
            </div>
            <Separator />
            <div className="space-y-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <h3 className="font-semibold">Images</h3>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() =>
                    setImages((v) => [
                      ...v,
                      { url: "", caption: "", isPrimary: v.length === 0 },
                    ])
                  }
                >
                  <Plus className="size-4" />
                  Add image
                </Button>
              </div>
              {!images.length && (
                <p className="text-sm text-muted-foreground">
                  No images. Products without images use a neutral placeholder.
                </p>
              )}
              {images.map((im, i) => (
                <div key={i} className="space-y-3 rounded-lg border p-4">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-medium">Image {i + 1}</p>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      aria-label={`Remove image ${i + 1}`}
                      onClick={() =>
                        setImages((v) => v.filter((_, index) => index !== i))
                      }
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor={`image-url-${i}`}>Image URL</Label>
                    <Input
                      id={`image-url-${i}`}
                      type="url"
                      required
                      value={im.url}
                      onChange={(e) => updateImage(i, { url: e.target.value })}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor={`image-caption-${i}`}>Caption</Label>
                    <Input
                      id={`image-caption-${i}`}
                      value={im.caption || ""}
                      onChange={(e) =>
                        updateImage(i, { caption: e.target.value })
                      }
                    />
                  </div>
                  <div className="flex items-center gap-3">
                    <Switch
                      id={`image-primary-${i}`}
                      checked={im.isPrimary}
                      onCheckedChange={(checked) =>
                        setImages((v) =>
                          v.map((image, index) => ({
                            ...image,
                            isPrimary: index === i ? checked : false,
                          })),
                        )
                      }
                    />
                    <Label htmlFor={`image-primary-${i}`}>Primary image</Label>
                  </div>
                </div>
              ))}
            </div>
            <Separator />
            <div className="grid gap-5 md:grid-cols-2">
              {(
                [
                  "highlights",
                  "facilities",
                  "inclusions",
                  "exclusions",
                ] as const
              ).map((key) => (
                <div key={key} className="space-y-2">
                  <Label htmlFor={key} className="capitalize">
                    {key}
                  </Label>
                  <Textarea
                    id={key}
                    name={key}
                    defaultValue={content?.[key]?.join("\n") || ""}
                    rows={4}
                    aria-describedby={`${key}-hint`}
                  />
                  <p
                    id={`${key}-hint`}
                    className="text-xs text-muted-foreground"
                  >
                    One item per line.
                  </p>
                </div>
              ))}
            </div>
            <div className="space-y-2">
              <Label htmlFor="additionalInfoJson">
                Additional information (JSON)
              </Label>
              <Textarea
                id="additionalInfoJson"
                name="additionalInfoJson"
                className="font-mono text-xs"
                defaultValue={content?.additionalInfoJson || ""}
                rows={3}
              />
              <p className="text-xs text-muted-foreground">
                Optional supplier or product information. Existing JSON is
                preserved unless edited.
              </p>
            </div>
          </fieldset>
          {error && <ErrorState message={error} />}
          <div className="flex flex-wrap items-center justify-between gap-3">
            <Button type="submit" disabled={busy}>
              {busy ? "Saving…" : "Save content"}
            </Button>
            {content && (
              <ConfirmDelete
                disabled={busy}
                label="Delete content"
                title="Delete this product’s content?"
                description="This removes the stored description, images and content lists. The imported product and its booking options remain available. Reimporting may recreate supplier content."
                action={async () => {
                  lock.current = true;
                  setBusy(true);
                  try {
                    await management.deleteContent(productId);
                  } finally {
                    lock.current = false;
                    setBusy(false);
                  }
                }}
                onDone={onDone}
              />
            )}
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
