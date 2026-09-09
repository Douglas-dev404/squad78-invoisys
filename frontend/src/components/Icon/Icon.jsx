/**
 * Componente de ícone agnóstico baseado em Material Symbols Outlined.
 * Suporta variante preenchida (filled) via prop boolean.
 *
 * @param {Object} props
 * @param {string} props.name - Nome do glifo no Material Symbols
 * @param {number} [props.size=24] - Tamanho em pixels
 * @param {string} [props.className] - Classes CSS adicionais
 * @param {boolean} [props.ariaHidden=true] - Se deve ser ocultado de leitores de tela
 * @param {string} [props.ariaLabel] - Rótulo acessível opcional
 * @param {boolean} [props.filled=false] - Usa a variação preenchida (FILL 1) do ícone
 */
export function Icon({
  name,
  size = 24,
  className = '',
  ariaHidden = true,
  ariaLabel,
  filled = false,
}) {
  return (
    <span
      className={`material-symbols-outlined select-none inline-flex items-center justify-center leading-none ${className}`}
      style={{
        fontSize: `${size}px`,
        fontVariationSettings: filled
          ? "'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24"
          : "'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24",
      }}
      aria-hidden={ariaHidden}
      aria-label={ariaLabel}
    >
      {name}
    </span>
  );
}
