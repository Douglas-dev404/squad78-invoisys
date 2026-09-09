import { useState } from 'react';

export function SearchBar({ onSearch, isLoading }) {
  const [query, setQuery] = useState('');

  const handleInputChange = (e) => {
    setQuery(e.target.value);
  };

  const handleSearch = () => {
    onSearch(query);
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  return (
    <div className="mb-lg bg-surface-container-lowest border border-surface-variant/50 rounded-xl p-xs shadow-[0px_4px_12px_rgba(27,67,50,0.08)] flex items-center justify-between gap-sm transition-all focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/20">
      <div className="flex items-center gap-sm flex-1 pl-sm">
        <span className="material-symbols-outlined text-outline">search</span>
        <input
          type="text"
          placeholder="Buscar tarefas por título, ID do Jira ou responsável..."
          value={query}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown}
          className="w-full bg-transparent border-0 p-0 font-body-sm text-body-sm text-on-surface placeholder:text-outline focus:ring-0 focus:outline-none"
          aria-label="Buscar tarefas"
        />
      </div>
      <div className="flex items-center gap-xs pr-xs">
        <span className="hidden md:inline-flex items-center px-2 py-1 rounded bg-surface-container font-label-sm text-[11px] text-on-surface-variant font-medium border border-outline-variant/30">
          ⌘K
        </span>
        <button
          onClick={handleSearch}
          disabled={isLoading}
          aria-busy={isLoading}
          className="bg-primary text-on-primary px-sm py-xs rounded-lg font-label-md text-label-md hover:bg-primary/90 transition-colors flex items-center gap-1 shadow-sm disabled:opacity-70 disabled:cursor-not-allowed"
        >
          <span>Buscar</span>
        </button>
      </div>
    </div>
  );
}
