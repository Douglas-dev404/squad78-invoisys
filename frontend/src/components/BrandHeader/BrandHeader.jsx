import { Icon } from '../Icon/Icon';

/**
 * Componente do cabeçalho da marca (Logo e Nome).
 * Componente puramente apresentacional ("burro").
 * @param {Object} props
 * @param {string} props.name - Nome da marca/sistema
 * @param {string} props.iconName - Nome do ícone representativo
 */
export function BrandHeader({ name, iconName = 'energy_savings_leaf' }) {
  return (
    <header className="flex items-center gap-3 mb-xl">
      <div
        className="w-10 h-10 bg-primary-container rounded-lg flex items-center justify-center text-on-primary shadow-sm"
        aria-hidden="true"
      >
        <Icon name={iconName} size={24} />
      </div>
      <h1 className="font-headline-lg text-headline-lg text-primary tracking-tight">
        {name}
      </h1>
    </header>
  );
}
