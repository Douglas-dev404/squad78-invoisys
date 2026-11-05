import { fetchProfile } from '../services/profile.service';
import { useAsyncResource } from './useAsyncResource';

export function useProfile() {
  const resource = useAsyncResource(fetchProfile);
  return { ...resource, profile: resource.data };
}
