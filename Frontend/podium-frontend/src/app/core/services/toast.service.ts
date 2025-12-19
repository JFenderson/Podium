import { Injectable, inject } from '@angular/core';
import { ToastrService } from 'ngx-toastr';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private toastr = inject(ToastrService);

  success(message: string) {
    this.toastr.success(message, 'Success');
  }

  error(message: string) {
    this.toastr.error(message, 'Error');
  }
}