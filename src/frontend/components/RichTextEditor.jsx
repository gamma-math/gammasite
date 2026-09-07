import { useEffect, useRef, useState } from "react";
import { AlignCenter, AlignLeft, AlignRight, Link as LinkIcon } from "lucide-react";
import { editorApi } from "../services/api.js";
import { sanitizeHtml } from "../utils/richText.js";

/**
 * Shared rich text editor for content, messages, and email templates.
 */
export function RichTextEditor({ value, onChange, label = "Indhold", snippets = [], className = "", sanitize = true }) {
  const editorRef = useRef(null);
  const sourceRef = useRef(null);
  const uploadRef = useRef(null);
  const selectionRef = useRef(null);
  const lastEditorValue = useRef(null);
  const [sourceMode, setSourceMode] = useState(false);
  const [dialog, setDialog] = useState(null);
  const [imageUrl, setImageUrl] = useState("");
  const [linkUrl, setLinkUrl] = useState("");
  const [linkText, setLinkText] = useState("");
  const [selectedImage, setSelectedImage] = useState(null);
  const [imageWidth, setImageWidth] = useState(100);
  const [imageAlignment, setImageAlignment] = useState("left");
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!sourceMode && editorRef.current && lastEditorValue.current !== value) {
      editorRef.current.innerHTML = sanitize ? sanitizeHtml(value ?? "") : (value ?? "");
      lastEditorValue.current = value ?? "";
    }
  }, [value, sourceMode]);

  function updateValueFromEditor() {
    const nextValue = editorRef.current?.innerHTML ?? "";
    lastEditorValue.current = nextValue;
    onChange(nextValue);
  }

  function rememberSelection() {
    const selection = window.getSelection();
    if (selection?.rangeCount && editorRef.current?.contains(selection.anchorNode)) {
      selectionRef.current = selection.getRangeAt(0).cloneRange();
    }
  }

  function restoreSelection() {
    if (!selectionRef.current) return;
    const selection = window.getSelection();
    selection.removeAllRanges();
    selection.addRange(selectionRef.current);
  }

  function exec(command, commandValue = null) {
    restoreSelection();
    editorRef.current?.focus();
    document.execCommand(command, false, commandValue);
    updateValueFromEditor();
  }

  function handleEditorClick(event) {
    if (event.target.tagName !== "IMG") {
      setSelectedImage(null);
      return;
    }

    const width = Number.parseInt(event.target.getAttribute("width"), 10);
    const style = event.target.style;
    const alignment = style.marginLeft === "auto" && style.marginRight === "auto"
      ? "center"
      : style.marginLeft === "auto" ? "right" : "left";
    setSelectedImage(event.target);
    setImageWidth(Number.isFinite(width) ? Math.min(100, Math.max(10, width)) : 100);
    setImageAlignment(alignment);
  }

  function updateImageWidth(nextWidth) {
    if (!selectedImage) return;
    const width = Math.min(100, Math.max(10, Number(nextWidth)));
    selectedImage.setAttribute("width", `${width}%`);
    setImageWidth(width);
    updateValueFromEditor();
  }

  function updateImageAlignment(alignment) {
    if (!selectedImage) return;
    selectedImage.style.display = "block";
    selectedImage.style.marginLeft = alignment === "right" || alignment === "center" ? "auto" : "0";
    selectedImage.style.marginRight = alignment === "left" || alignment === "center" ? "auto" : "0";
    setImageAlignment(alignment);
    updateValueFromEditor();
  }

  function insertAtSource(html) {
    const source = sourceRef.current;
    const currentValue = value ?? "";
    const start = source?.selectionStart ?? currentValue.length;
    const end = source?.selectionEnd ?? currentValue.length;
    onChange(`${currentValue.slice(0, start)}${html}${currentValue.slice(end)}`);
    window.requestAnimationFrame(() => {
      source?.focus();
      source?.setSelectionRange(start + html.length, start + html.length);
    });
  }

  function insertImage(url) {
    if (!url.trim()) return;
    const image = `<img src="${escapeAttribute(url)}" alt="" />`;
    if (sourceMode) insertAtSource(image);
    else exec("insertHTML", image);
    setImageUrl("");
    setDialog(null);
  }

  function insertLink() {
    if (!linkUrl.trim()) return;
    const text = linkText.trim() || linkUrl.trim();
    const link = `<a href="${escapeAttribute(linkUrl.trim())}">${escapeAttribute(text)}</a>`;
    if (sourceMode) insertAtSource(link);
    else exec("insertHTML", link);
    setLinkUrl("");
    setLinkText("");
    setDialog(null);
  }

  async function uploadImage(event) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    setError("");
    setIsUploading(true);
    try {
      const result = await editorApi.uploadImage(file);
      insertImage(result.url);
    } catch (reason) {
      setError(reason.message);
    } finally {
      setIsUploading(false);
    }
  }

  function insertSnippet(snippet) {
    if (sourceMode) {
      insertAtSource(snippet);
      return;
    }
    exec("insertText", snippet);
  }

  function openLinkDialog() {
    rememberSelection();
    setLinkText(window.getSelection()?.toString() ?? "");
    setDialog("link");
  }

  function openImageDialog() {
    rememberSelection();
    setDialog("image");
  }

  function toggleSource() {
    if (!sourceMode) {
      updateValueFromEditor();
    } else {
      lastEditorValue.current = null;
    }
    setSourceMode((current) => !current);
  }

  const availableSnippets = [...new Set([...snippets, "{{Name}}"])] ;

  return (
    <div className={`admin-rich-editor ${snippets.length > 0 ? "admin-rich-editor-with-snippets" : ""} ${className}`.trim()}>
      <span className="admin-field-label">{label}</span>
      <div className="admin-rich-toolbar" role="toolbar" aria-label="Tekstformatering">
        <select className="admin-rich-format" defaultValue="P" onMouseDown={rememberSelection} onChange={(event) => exec("formatBlock", event.target.value)} aria-label="Teksttype">
          <option value="P">Normal</option>
          <option value="H2">Overskrift 2</option>
          <option value="H3">Overskrift 3</option>
          <option value="BLOCKQUOTE">Citat</option>
        </select>
        <select className="admin-rich-font" defaultValue="Source Sans 3" onMouseDown={rememberSelection} onChange={(event) => exec("fontName", event.target.value)} aria-label="Skrifttype">
          <option value="Source Sans 3">Source Sans 3</option>
          <option value="Sora">Sora</option>
          <option value="Arial">Arial</option>
          <option value="Georgia">Georgia</option>
          <option value="Courier New">Courier New</option>
        </select>
        <button type="button" onClick={() => exec("bold")} aria-label="Fed">B</button>
        <button type="button" onClick={() => exec("italic")} aria-label="Kursiv"><em>I</em></button>
        <button type="button" onClick={() => exec("underline")} aria-label="Understregning"><u>U</u></button>
        <button type="button" onClick={() => exec("strikeThrough")} aria-label="Gennemstregning"><s>S</s></button>
        <button type="button" onClick={() => exec("foreColor", "#1f2d3d")} aria-label="Tekstfarve">A</button>
        <button type="button" onClick={() => exec("justifyLeft")} aria-label="Venstrejuster"><AlignLeft size={16} /></button>
        <button type="button" onClick={() => exec("justifyCenter")} aria-label="Centrer"><AlignCenter size={16} /></button>
        <button type="button" onClick={() => exec("justifyRight")} aria-label="Højrejuster"><AlignRight size={16} /></button>
        <button type="button" onClick={() => exec("insertUnorderedList")} aria-label="Punktliste">&#8226;&#8226;&#8226;</button>
        <button type="button" onClick={() => exec("insertOrderedList")} aria-label="Nummereret liste">1.2.3.</button>
        <button type="button" onMouseDown={(event) => event.preventDefault()} onClick={openLinkDialog} aria-label="Indsæt link"><LinkIcon size={16} /></button>
        <button type="button" onMouseDown={(event) => event.preventDefault()} onClick={openImageDialog} aria-label="Indsæt billede">Billede</button>
        <button type="button" className={sourceMode ? "is-active" : ""} onClick={toggleSource} aria-label="Vis HTML">&lt;/&gt;</button>
      </div>
      {snippets.length > 0 && (
        <div className="admin-rich-snippet-toolbar" role="toolbar" aria-label="Indsæt variabelblokke">
          <span>Indsæt blok:</span>
          <select defaultValue="" onChange={(event) => {
            if (event.target.value) insertSnippet(event.target.value);
            event.target.value = "";
          }} aria-label="Vælg blok">
            <option value="">Vælg blok</option>
            {availableSnippets.map((snippet) => <option value={snippet} key={snippet}>{snippet}</option>)}
          </select>
        </div>
      )}
      {error && <p className="status-message status-message-error">{error}</p>}
      {sourceMode ? (
        <textarea ref={sourceRef} className="admin-rich-source" value={value ?? ""} onChange={(event) => onChange(event.target.value)} aria-label="HTML-kilde" />
      ) : (
        <div ref={editorRef} className="admin-rich-input" contentEditable suppressContentEditableWarning onInput={updateValueFromEditor} onClick={handleEditorClick} />
      )}
      {selectedImage && !sourceMode && (
        <div className="admin-rich-image-size" aria-label="Billedstørrelse">
          <span>Billedstørrelse</span>
          <button type="button" onClick={() => updateImageWidth(imageWidth - 10)} aria-label="Gør billede mindre">−</button>
          <input type="range" min="10" max="100" step="5" value={imageWidth} onChange={(event) => updateImageWidth(event.target.value)} />
          <button type="button" onClick={() => updateImageWidth(imageWidth + 10)} aria-label="Gør billede større">+</button>
          <output>{imageWidth}%</output>
          <button type="button" onClick={() => updateImageWidth(100)}>Nulstil</button>
          <span>Placering</span>
          <button type="button" className={imageAlignment === "left" ? "is-active" : ""} onClick={() => updateImageAlignment("left")} aria-label="Placer til venstre"><AlignLeft size={15} /></button>
          <button type="button" className={imageAlignment === "center" ? "is-active" : ""} onClick={() => updateImageAlignment("center")} aria-label="Centrer billede"><AlignCenter size={15} /></button>
          <button type="button" className={imageAlignment === "right" ? "is-active" : ""} onClick={() => updateImageAlignment("right")} aria-label="Placer til højre"><AlignRight size={15} /></button>
        </div>
      )}
      {dialog && (
        <div className="admin-rich-dialog-backdrop" role="presentation" onClick={() => setDialog(null)}>
          <section className="admin-rich-dialog" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
            <div className="admin-rich-dialog-header">
              <strong>{dialog === "image" ? "Indsæt billede" : "Indsæt link"}</strong>
              <button type="button" onClick={() => setDialog(null)} aria-label="Luk">×</button>
            </div>
            {dialog === "image" ? (
              <>
                <label className="admin-field"><span>Billede URL</span><input autoFocus value={imageUrl} onChange={(event) => setImageUrl(event.target.value)} placeholder="https://..." /></label>
                <div className="admin-rich-dialog-actions">
                  <button className="admin-action-button" type="button" onClick={() => insertImage(imageUrl)} disabled={!imageUrl.trim()}>Indsæt URL</button>
                  <button className="admin-action-button" type="button" onClick={() => uploadRef.current?.click()} disabled={isUploading}>{isUploading ? "Uploader..." : "Vælg fil"}</button>
                </div>
                <input ref={uploadRef} type="file" accept="image/*" onChange={uploadImage} hidden />
              </>
            ) : (
              <>
                <label className="admin-field"><span>Link</span><input autoFocus value={linkUrl} onChange={(event) => setLinkUrl(event.target.value)} placeholder="https://..." /></label>
                <label className="admin-field"><span>Linktekst</span><input value={linkText} onChange={(event) => setLinkText(event.target.value)} placeholder="Teksten der skal vises" /></label>
                <div className="admin-rich-dialog-actions"><button className="profile-button" type="button" onClick={insertLink} disabled={!linkUrl.trim()}>Indsæt link</button></div>
              </>
            )}
          </section>
        </div>
      )}
    </div>
  );
}

function escapeAttribute(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll('"', "&quot;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;");
}
