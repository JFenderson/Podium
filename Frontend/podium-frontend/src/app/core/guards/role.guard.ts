import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { AuthService } from '../services/auth';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard  {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): Observable<boolean | UrlTree> {
    const requiredRoles = route.data['roles'] as string[];
    const requiredPermissions = route.data['permissions'] as string[];

    return this.authService.currentUser$.pipe(
      take(1),
      map(user => {
        if (!user) {
          return this.router.createUrlTree(['/auth/login']);
        }

        // Check roles if specified
        if (requiredRoles && requiredRoles.length > 0) {
          const hasRole = requiredRoles.includes(user.role);
          if (!hasRole) {
            return this.router.createUrlTree(['/unauthorized']);
          }
        }

        // Check permissions if specified
        if (requiredPermissions && requiredPermissions.length > 0) {
          const hasPermission = requiredPermissions.some(permission => 
            user.permissions?.includes(permission)
          );
          if (!hasPermission) {
            return this.router.createUrlTree(['/unauthorized']);
          }
        }

        return true;
      })
    );
  }

  canActivateChild(route: ActivatedRouteSnapshot): Observable<boolean | UrlTree> {
    return this.canActivate(route);
  }
}