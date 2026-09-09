import { Icon } from '../Icon/Icon';

/**
 * Componente HeroCard representando um item de destaque da plataforma.
 * Renderizado em lista via .map() com chave estável (id).
 * @param {Object} props
 * @param {string} props.title - Título do destaque
 * @param {string} props.description - Descrição do destaque
 * @param {string} [props.icon='task_alt'] - Nome do ícone
 */
export function HeroCard({ title, description, icon = 'task_alt' }) {
  return (
    <article className="bg-primary/20 backdrop-blur-sm border border-on-primary-container/20 p-5 rounded-xl text-left transition-all hover:bg-primary/30">
      <div className="flex items-start gap-4">
        <div
          className="w-10 h-10 rounded-lg bg-secondary-container/20 text-on-primary flex items-center justify-center shrink-0"
          aria-hidden="true"
        >
          <Icon name={icon} size={22} />
        </div>
        <div>
          <h3 className="font-headline-sm text-on-primary text-base font-semibold mb-1">
            {title}
          </h3>
          <p className="font-body-sm text-body-sm text-on-primary-container">
            {description}
          </p>
        </div>
      </div>
    </article>
  );
}
