import { HeroCard } from '../HeroCard/HeroCard';
import { Button } from '../Button/Button';
import { Icon } from '../Icon/Icon';
import { UI_STRINGS } from '../../utils/constants';

/**
 * Seção lateral do Hero (apresentação visual e comunicados).
 * Componente "burro": recebe estado e dados inteiramente via props.
 * Trata e renderiza os 3 estados: carregamento, erro e lista vazia.
 *
 * @param {Object} props
 * @param {Array<{ id: string, title: string, description: string, icon: string }>} props.highlights - Lista de destaques
 * @param {boolean} [props.isLoading=false] - Estado de carregamento
 * @param {string|null} [props.error=null] - Mensagem de erro ao carregar dados
 * @param {Function} [props.onRetry] - Callback para tentar recarregar em caso de erro
 * @param {string} [props.mainTitle] - Título principal
 * @param {string} [props.mainSubtitle] - Subtítulo principal
 */
export function HeroSection({
  highlights = [],
  isLoading = false,
  error = null,
  onRetry,
  mainTitle = 'Gestão automatizada de tasks',
  mainSubtitle = 'Simplifique sua produtividade.',
}) {
  return (
    <aside
      aria-label="Apresentação institucional Invoisys"
      className="hidden md:block w-full md:w-7/12 lg:w-8/12 h-screen relative bg-surface-container-low overflow-hidden"
    >
      <div className="flex flex-col items-center justify-center h-full p-gutter text-center bg-primary-container overflow-y-auto">
        <div className="max-w-xl w-full flex flex-col items-center">
          {/* Título e subtítulo do Hero */}
          <header className="mb-8">
            <h2 className="font-headline-lg text-headline-lg text-on-primary mb-3 tracking-tight">
              {mainTitle}
            </h2>
            <p className="font-body-lg text-body-lg text-on-primary-container">
              {mainSubtitle}
            </p>
          </header>

          {/* ESTADO 1: Carregando (Loading Skeleton) */}
          {isLoading && (
            <div
              role="status"
              aria-live="polite"
              className="w-full flex flex-col gap-4 animate-pulse"
            >
              {[1, 2].map((placeholderId) => (
                <div
                  key={`skeleton-${placeholderId}`}
                  className="bg-primary/20 p-5 rounded-xl h-20 w-full"
                />
              ))}
              <span className="sr-only">Carregando destaques institucionais...</span>
            </div>
          )}

          {/* ESTADO 2: Erro com opção de Retry */}
          {!isLoading && error && (
            <div
              role="alert"
              className="w-full bg-primary/40 border border-error/40 p-5 rounded-xl text-on-primary flex flex-col items-center gap-3"
            >
              <Icon name="cloud_off" size={28} className="text-on-primary-container" />
              <p className="text-body-sm text-on-primary font-medium">{error}</p>
              {onRetry && (
                <Button
                  onClick={onRetry}
                  className="!bg-secondary-container !text-on-secondary-container hover:!bg-secondary hover:!text-on-primary text-sm py-2 px-4 shadow-none"
                >
                  {UI_STRINGS.ERROR_RETRY_BUTTON}
                </Button>
              )}
            </div>
          )}

          {/* ESTADO 3: Lista Vazia */}
          {!isLoading && !error && highlights.length === 0 && (
            <div className="w-full bg-primary/20 border border-on-primary-container/20 p-6 rounded-xl text-on-primary-container text-center">
              <Icon name="inbox" size={32} className="mb-2" />
              <p className="font-headline-sm text-base text-on-primary font-semibold">
                {UI_STRINGS.EMPTY_HIGHLIGHTS_TITLE}
              </p>
              <p className="font-body-sm text-body-sm mt-1">
                {UI_STRINGS.EMPTY_HIGHLIGHTS_SUBTITLE}
              </p>
            </div>
          )}

          {/* ESTADO 4: Lista com dados renderizada via .map() com key estável (item.id) */}
          {!isLoading && !error && highlights.length > 0 && (
            <div className="w-full flex flex-col gap-4">
              {highlights.map((item) => (
                <HeroCard
                  key={item.id}
                  title={item.title}
                  description={item.description}
                  icon={item.icon}
                />
              ))}
            </div>
          )}
        </div>
      </div>
    </aside>
  );
}
