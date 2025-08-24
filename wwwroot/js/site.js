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
    const suffix = `Perspective: ${
      String(perspective).toLowerCase() === "opp" ? "Opponents" : "Team Itself"
    }`;
    return initStatDescription(selectId, targetId, descriptions, suffix);
  }

  // Exponer API pública
  window.StrikeData.initStatDescription = initStatDescription;
  window.StrikeData.initCuriousFactsDescription = initCuriousFactsDescription;
})();
