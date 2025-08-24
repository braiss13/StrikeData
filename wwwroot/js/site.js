/**
 * StrikeData UI helpers
 * Reusable initializer for "stat description under a <select>" pattern.
 * - selectId: id del <select> (o el propio elemento)
 * - targetId: id del elemento donde escribir la descripción (o el propio elemento)
 * - descriptions: objeto { [value]: "Descripción" } generado desde Razor
 * - extraSuffix: texto adicional opcional que se añade al final
 */
(function () {
  window.StrikeData = window.StrikeData || {};

  function initStatDescription(selectId, targetId, descriptions, extraSuffix) {
    const sel =
      typeof selectId === "string"
        ? document.getElementById(selectId)
        : selectId;

    const out =
      typeof targetId === "string"
        ? document.getElementById(targetId)
        : targetId;

    if (!sel || !out) return;

    function update() {
      const val = sel.value;
      const base =
        (descriptions && (descriptions[val] || descriptions[String(val)])) ||
        "";
      out.textContent = base
        ? extraSuffix
          ? `${base} — ${extraSuffix}`
          : base
        : "";
    }

    sel.addEventListener("change", update);
    // Primera pintura tras cargar la página
    update();

    // Devuelve una API mínima por si se quiere forzar la actualización externamente
    return { update };
  }

  /**
   * Atajo específico para Curious Facts:
   * añade automáticamente "Perspective: Team Itself|Opponents" al final.
   */
  function initCuriousFactsDescription(
    selectId,
    targetId,
    descriptions,
    perspective
  ) {
    const suffix = `Perspective: ${String(perspective).toLowerCase() === "opp" ? "Opponents" : "Team Itself"
      }`;
    return initStatDescription(selectId, targetId, descriptions, suffix);
  }

  // Exponer API pública
  window.StrikeData.initStatDescription = initStatDescription;
  window.StrikeData.initCuriousFactsDescription = initCuriousFactsDescription;
})();


(function () {
  // Inicializa ordenación para todas las tablas con clase .sortable-table
  window.StrikeData.initTableSorting = function () {
    document
      .querySelectorAll("table.sortable-table thead th[data-sortable]")
      .forEach(function (th) {
        let asc = true;
        th.addEventListener("click", function () {
          const table = th.closest("table");
          const tbody = table.querySelector("tbody");
          const index = Array.from(th.parentNode.children).indexOf(th);
          const type = th.dataset.type || "string";
          const rows = Array.from(tbody.querySelectorAll("tr"));

          function dateKey(txt) {
            // espera exactamente yyyy-MM-dd
            const m = /^\s*(\d{4})-(\d{2})-(\d{2})\s*$/.exec(txt);
            if (!m) return NaN;
            return (+m[1]) * 10000 + (+m[2]) * 100 + (+m[3]); // yyyymmdd numérico
          }

          rows.sort(function (a, b) {
            const cellA = (a.children[index]?.innerText || "").trim();
            const cellB = (b.children[index]?.innerText || "").trim();
            let comp;

            if (type === "number") {
              const aNum = parseFloat(cellA.replace(",", "."));
              const bNum = parseFloat(cellB.replace(",", "."));
              const aa = Number.isNaN(aNum) ? Number.NEGATIVE_INFINITY : aNum;
              const bb = Number.isNaN(bNum) ? Number.NEGATIVE_INFINITY : bNum;
              comp = aa - bb;
            } else if (type === "date") {
              const ka = dateKey(cellA);
              const kb = dateKey(cellB);
              const aa = Number.isNaN(ka) ? Number.NEGATIVE_INFINITY : ka;
              const bb = Number.isNaN(kb) ? Number.NEGATIVE_INFINITY : kb;
              comp = aa - bb;
            } else {
              comp = cellA.localeCompare(cellB);
            }

            return asc ? comp : -comp;
          });

          asc = !asc;
          rows.forEach((row) => tbody.appendChild(row));
        });
      });
  };

  // Llama a la función al cargar la página
  document.addEventListener("DOMContentLoaded", function () {
    window.StrikeData.initTableSorting();
  });
})();
