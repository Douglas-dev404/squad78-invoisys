/**
 * Componente apresentacional para o cartão com logotipo da Invoisys.
 * @param {Object} props
 * @param {string} props.logoUrl - URL da imagem do logotipo
 * @param {string} [props.altText='Invoisys Logo'] - Texto alternativo para acessibilidade
 */
export function BrandLogoCard({ logoUrl, altText = 'Invoisys Logo' }) {
  return (
    <div className="w-16 h-16 mb-md bg-surface-container-lowest rounded-lg shadow-sm border border-outline-variant/20 flex items-center justify-center overflow-hidden">
      <img
        src={logoUrl}
        alt={altText}
        className="w-full h-full object-contain p-2"
        loading="eager"
      />
    </div>
  );
}
