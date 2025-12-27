import { Directive, HostListener, Input, ElementRef } from '@angular/core';

export interface KeyboardShortcut {
  key: string;
  ctrl?: boolean;
  shift?: boolean;
  alt?: boolean;
  action: () => void;
  description: string;
}

@Directive({
  selector: '[appKeyboardNavigation]',
  standalone: true
})
export class KeyboardNavigationDirective {
  @Input() shortcuts: KeyboardShortcut[] = [];

  constructor(private el: ElementRef) {}

  @HostListener('window:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent) {
    const matchingShortcut = this.shortcuts.find(shortcut => 
      shortcut.key.toLowerCase() === event.key.toLowerCase() &&
      (shortcut.ctrl === undefined || shortcut.ctrl === event.ctrlKey) &&
      (shortcut.shift === undefined || shortcut.shift === event.shiftKey) &&
      (shortcut.alt === undefined || shortcut.alt === event.altKey)
    );

    if (matchingShortcut) {
      event.preventDefault();
      matchingShortcut.action();
    }
  }

  // Focus trap for modals
  @HostListener('keydown.tab', ['$event'])
  @HostListener('keydown.shift.tab', ['$event'])
  handleTabKey(event: Event) {
    const keyboardEvent = event as KeyboardEvent;
    const focusableElements = this.el.nativeElement.querySelectorAll(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );


    if (focusableElements.length === 0) return;

    const firstElement = focusableElements[0] as HTMLElement;
    const lastElement = focusableElements[focusableElements.length - 1] as HTMLElement;

    if (keyboardEvent.shiftKey) {
      if (document.activeElement === firstElement) {
        lastElement.focus();
        keyboardEvent.preventDefault();
      }
    } else {
      if (document.activeElement === lastElement) {
        firstElement.focus();
        keyboardEvent.preventDefault();
      }
    }
  }
}