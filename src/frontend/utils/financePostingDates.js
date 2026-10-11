/**
 * Returns the date used for financial aggregates when a posting does not
 * have an explicit posting date yet.
 */
export function effectivePostingDate(postingDate, transactionDate) {
  return postingDate || transactionDate || "";
}
