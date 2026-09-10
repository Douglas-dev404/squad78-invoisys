import { useState, useCallback } from 'react';

export function useSidebarMinimize() {
  const [isMinimized, setIsMinimized] = useState(false);

  const toggle = useCallback(() => {
    setIsMinimized((prev) => !prev);
  }, []);

  return {
    isMinimized,
    toggle,
  };
}
