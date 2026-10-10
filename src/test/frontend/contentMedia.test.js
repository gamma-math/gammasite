import test from "node:test";
import assert from "node:assert/strict";
import { isLocalContentImage, isSupportedContentImage, MAX_CONTENT_IMAGE_SIZE } from "../../frontend/utils/contentMedia.js";

test("accepts PNG and JPEG names up to 2 MiB", () => {
  assert.equal(isSupportedContentImage({ name: "photo.PNG", size: MAX_CONTENT_IMAGE_SIZE }), true);
  assert.equal(isSupportedContentImage({ name: "photo.jpeg", size: MAX_CONTENT_IMAGE_SIZE }), true);
});

test("rejects unsupported extensions and oversized files before upload", () => {
  assert.equal(isSupportedContentImage({ name: "photo.webp", size: 100 }), false);
  assert.equal(isSupportedContentImage({ name: "photo.jpg", size: MAX_CONTENT_IMAGE_SIZE + 1 }), false);
});

test("recognizes only generated local content image URLs", () => {
  assert.equal(isLocalContentImage("/media/content/events/event-123.jpg"), true);
  assert.equal(isLocalContentImage("https://images.example/event.jpg"), false);
  assert.equal(isLocalContentImage("/uploads/editor/event.jpg"), false);
});
