/**
 * StrikeData UI helpers
 * Reusable initializer for the "stat description under a <select>" pattern.
 * - selectId: id of the <select> element, or the element itself
 * - targetId: id of the output element for the description, or the element itself
 * - descriptions: plain object { [value]: "Description" } generated server-side (Razor)
 * - extraSuffix: optional string appended to the resolved description
 */
(function () {
  // Ensure a single global namespace for StrikeData helpers.
  window.StrikeData = window.StrikeData || {};

  /**
   * Wires a <select> to a target element so that, when the selected option changes,
   * the corresponding description is displayed. Descriptions are looked up by the
   * selected value (stringified as needed).
   */
  function initStatDescription(selectId, targetId, descriptions, extraSuffix) {
    // Accept either a string id or a direct element reference.
    const sel =
      typeof selectId === "string"
        ? document.getElementById(selectId)
        : selectId;

    const out =
      typeof targetId === "string"
        ? document.getElementById(targetId)
        : targetId;

    // If either endpoint is missing, do nothing (safe no-op).
    if (!sel || !out) return;

    // Updates the output text based on the current <select> value.
    function update() {
      const val = sel.value;

      // Try both raw and stringified keys to be tolerant with server rendering.
      const base =
        (descriptions && (descriptions[val] || descriptions[String(val)])) ||
        "";

      // Append the optional suffix only when a base description exists.
      out.textContent = base
        ? extraSuffix
          ? `${base} — ${extraSuffix}`
          : base
        : "";
    }

    // React to user interaction.
    sel.addEventListener("change", update);

    // Initial paint on page load so the description matches the initial selection.
    update();

    // Return a minimal API in case the caller needs to force-refresh later.
    return { update };
  }

  /**
   * Convenience wrapper for "Curious Facts" pages:
   * it automatically appends the perspective suffix:
   *   - "Team Itself" (default)
   *   - "Opponents"   (when perspective === "opp")
   */
  function initCuriousFactsDescription(
    selectId,
    targetId,
    descriptions,
    perspective
  ) {
    const suffix = `Perspective: ${
      String(perspective).toLowerCase() === "opp" ? "Opponents" : "Team Itself"
    }`;
    return initStatDescription(selectId, targetId, descriptions, suffix);
  }

  // Public API
  window.StrikeData.initStatDescription = initStatDescription;
  window.StrikeData.initCuriousFactsDescription = initCuriousFactsDescription;
})();

/**
 * Table sorting helpers
 * Adds click-to-sort behavior to any table header cell (<th>) that declares:
 *   - data-sortable   : presence enables sorting
 *   - data-type       : "number" | "date" | "string" (default: "string")
 * Rows are re-appended in sorted order; clicking the same header toggles direction.
 */
(function () {
  // Expose a function to (re)initialize sorting across all eligible tables.
  window.StrikeData.initTableSorting = function () {
    document
      .querySelectorAll("table.sortable-table thead th[data-sortable]")
      .forEach(function (th) {
        // Direction toggle stored per-header; defaults to ascending.
        let asc = true;

        th.addEventListener("click", function () {
          const table = th.closest("table");
          const tbody = table.querySelector("tbody");

          // Determine the clicked column index among its siblings.
          const index = Array.from(th.parentNode.children).indexOf(th);

          // Sorting strategy is chosen via data-type; fallback to string comparison.
          const type = th.dataset.type || "string";

          // Snapshot current rows; we'll reorder and re-append them.
          const rows = Array.from(tbody.querySelectorAll("tr"));

          // Lightweight key extractor for strict yyyy-MM-dd dates; returns a numeric yyyymmdd key.
          function dateKey(txt) {
            // expects exactly yyyy-MM-dd
            const m = /^\s*(\d{4})-(\d{2})-(\d{2})\s*$/.exec(txt);
            if (!m) return NaN;
            return (+m[1]) * 10000 + (+m[2]) * 100 + (+m[3]); // numeric yyyymmdd
          }

          rows.sort(function (a, b) {
            // Extract the cell text for the clicked column; trim to avoid whitespace noise.
            const cellA = (a.children[index]?.innerText || "").trim();
            const cellB = (b.children[index]?.innerText || "").trim();
            let comp;

            if (type === "number") {
              // Parse as float; treat NaN as -Infinity so unknowns sink to the bottom on ascending sort.
              const aNum = parseFloat(cellA.replace(",", "."));
              const bNum = parseFloat(cellB.replace(",", "."));
              const aa = Number.isNaN(aNum) ? Number.NEGATIVE_INFINITY : aNum;
              const bb = Number.isNaN(bNum) ? Number.NEGATIVE_INFINITY : bNum;
              comp = aa - bb;
            } else if (type === "date") {
              // Convert to a comparable numeric key; unknowns are pushed to the end.
              const ka = dateKey(cellA);
              const kb = dateKey(cellB);
              const aa = Number.isNaN(ka) ? Number.NEGATIVE_INFINITY : ka;
              const bb = Number.isNaN(kb) ? Number.NEGATIVE_INFINITY : kb;
              comp = aa - bb;
            } else {
              // Default string comparison (locale-aware for consistent A–Z ordering).
              comp = cellA.localeCompare(cellB);
            }

            return asc ? comp : -comp;
          });

          // Toggle sort direction for the next click on the same header.
          asc = !asc;

          // Re-append in the new order; the DOM move updates the visual order.
          rows.forEach((row) => tbody.appendChild(row));
        });
      });
  };

  // Initialize sorting after the DOM is ready so all tables/headers are present.
  document.addEventListener("DOMContentLoaded", function () {
    window.StrikeData.initTableSorting();
  });
})();
