import { useRef, useState } from "react";
import { X } from "lucide-react";
import { financeApi } from "../services/financeApi.js";

export function FinanceBudgetUploadDialog({ onClose, onSuccess }) {
  const fileInputRef = useRef(null);
  const [file, setFile] = useState(null);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const submit = async (event) => {
    event.preventDefault();
    setError("");
    setSuccess("");
    if (!file) {
      setError("Vælg en CSV-fil først.");
      return;
    }

    setIsUploading(true);
    try {
      const result = await financeApi.importAdminBudgets(file);
      const count = result?.importedCount ?? 0;
      setSuccess(`${count} budgetlinje${count === 1 ? "" : "r"} blev importeret.`);
      setFile(null);
      if (fileInputRef.current) fileInputRef.current.value = "";
      await onSuccess?.(result);
      onClose();
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <div
      className="admin-preview-modal-backdrop"
      role="presentation"
      onClick={() => !isUploading && onClose()}
    >
      <section
        className="admin-preview-modal finance-budget-upload-modal"
        role="dialog"
        aria-modal="true"
        aria-label="Upload budgetter"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="admin-preview-modal-header">
          <div>
            <p className="menu-section-title">Budgetter</p>
            <h2>Upload CSV</h2>
          </div>
          <button
            className="admin-preview-close"
            type="button"
            aria-label="Luk upload"
            onClick={onClose}
            disabled={isUploading}
          >
            <X size={20} />
          </button>
        </div>

        <form className="finance-budget-upload-body" onSubmit={submit}>
          <p>
            CSV-filen skal være semikolonsepareret og indeholde kolonnerne ID,
            konto, posteringsgruppe, år, beløb og budgettype.
          </p>
          <div className="finance-budget-upload-file">
            <input
              ref={fileInputRef}
              className="finance-import-file-input"
              type="file"
              accept=".csv,text/csv"
              onChange={(event) => {
                setFile(event.target.files?.[0] ?? null);
                setError("");
                setSuccess("");
              }}
            />
            <button
              className="profile-button finance-import-file-button"
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={isUploading}
            >
              Vælg CSV-fil
            </button>
            <span className="finance-import-file-name">
              {file ? file.name : "Ingen fil valgt"}
            </span>
          </div>
          <a
            className="finance-import-example-link"
            href="/api/finance/admin/budgets/template"
          >
            Download template
          </a>
          {error && (
            <p className="status-message status-message-error" role="alert">
              {error}
            </p>
          )}
          {success && (
            <p className="status-message status-message-success" role="status">
              {success}
            </p>
          )}
          <div className="menu-editor-actions">
            <button
              className="frontpage-button frontpage-button-secondary"
              type="button"
              onClick={onClose}
              disabled={isUploading}
            >
              Annuller
            </button>
            <button className="profile-button" type="submit" disabled={isUploading}>
              {isUploading ? "Uploader..." : "Upload budgetter"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
