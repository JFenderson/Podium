import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { StudentService } from '../../features/student/services/student.service';
import { map } from 'rxjs/operators';

export const incompleteProfileGuard: CanActivateFn = (route, state) => {
  const studentService = inject(StudentService);
  const router = inject(Router);

  return studentService.getMyProfile().pipe(
    map(profile => {
      // 1. If profile is missing or marked incomplete by backend
      const isIncomplete = !profile || profile.accountStatus === 'Incomplete';
      
      // 2. Or if critical fields are missing (Manual check)
      const hasCriticalFields = profile.firstName && profile.lastName && profile.primaryInstrument;

      if (!isIncomplete && hasCriticalFields) {
        return true;
      }

      // If we are already on the dashboard, don't redirect (avoids infinite loop)
      if (state.url.includes('/student/dashboard')) {
        return true;
      }

      // Redirect to dashboard and trigger wizard via query param
      return router.createUrlTree(['/student/dashboard'], { 
        queryParams: { setup: 'true' } 
      });
    })
  );
};