# Guia de Referência de Arquitetura Front-end (React + Tailwind)

Este guia contém os padrões de código e exemplos de referência que a skill utiliza para converter templates HTML/CSS em aplicações React modulares e preparadas para backend.

---

## 1. Padrão de Serviço Assíncrono (`src/services/*.service.js`)

```javascript
import { ENDPOINTS } from '../config/api.config';
import { MOCK_ITEMS } from '../data/items.mock';

export async function getItems() {
  // Simulação de latência de rede
  await new Promise((resolve) => setTimeout(resolve, 500));

  // TODO: Substituir mock por chamada HTTP real com fetch/axios:
  // const response = await fetch(ENDPOINTS.ITEMS.LIST);
  // if (!response.ok) throw new Error('Falha ao carregar registros.');
  // return await response.json();

  return [...MOCK_ITEMS];
}
```

---

## 2. Padrão de Custom Hook (`src/hooks/useItems.js`)

```javascript
import { useState, useEffect, useCallback } from 'react';
import { getItems } from '../services/items.service';

export function useItems() {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await getItems();
      setData(Array.isArray(items) ? items : []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro desconhecido.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch: fetchData };
}
```

---

## 3. Padrão de Tratamento dos 3 Estados em Componente Apresentacional

```jsx
export function ItemListSection({ items, isLoading, error, onRetry }) {
  if (isLoading) {
    return <SkeletonLoader />;
  }

  if (error) {
    return (
      <div role="alert">
        <p>{error}</p>
        {onRetry && <button onClick={onRetry}>Tentar novamente</button>}
      </div>
    );
  }

  if (items.length === 0) {
    return <EmptyState message="Nenhum item encontrado." />;
  }

  return (
    <div className="grid gap-4">
      {items.map((item) => (
        <ItemCard key={item.id} item={item} />
      ))}
    </div>
  );
}
```
