import { inject } from '@angular/core';
import { Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../../features/auth/services/auth';
import { CurrentUser } from '../models/auth';

export const roleGuard = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const user = authService.currentUserValue;
  if (!user) {
    router.navigate(['/login']);
    return false;
  }

  // Get required roles from route data
  const requiredRoles = route.data['roles'] as string[];
  const requiredPermissions = route.data['permissions'] as string[];

  if (requiredRoles?.length > 0) {
    const hasRole = requiredRoles.some(role => user.roles.includes(role));
    if (!hasRole) {
      router.navigate(['/unauthorized']);
      return false;
    }
  }

  if (requiredPermissions?.length > 0) {
    const hasPermission = requiredPermissions.every(permission =>
      user.permissions?.includes(permission)
    );
    if (!hasPermission) {
      router.navigate(['/unauthorized']);
      return false;
    }
  }

  return true;
};