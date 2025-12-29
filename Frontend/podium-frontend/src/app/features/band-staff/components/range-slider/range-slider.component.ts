import { Component, Input, Output, EventEmitter, OnInit, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface RangeValue {
  min: number;
  max: number;
}

@Component({
  selector: 'app-range-slider',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './range-slider.component.html',
  styleUrls: ['./range-slider.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RangeSliderComponent),
      multi: true
    }
  ]
})
export class RangeSliderComponent implements OnInit, ControlValueAccessor {
  @Input() min = 0;
  @Input() max = 100;
  @Input() step = 1;
  @Input() label = '';
  @Input() formatValue?: (value: number) => string;
  
  @Output() rangeChange = new EventEmitter<RangeValue>();

  currentMin = 0;
  currentMax = 100;
  
  private onChange: (value: RangeValue) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnInit(): void {
    this.currentMin = this.min;
    this.currentMax = this.max;
  }

  writeValue(value: RangeValue | null): void {
    if (value) {
      this.currentMin = value.min ?? this.min;
      this.currentMax = value.max ?? this.max;
    }
  }

  registerOnChange(fn: (value: RangeValue) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  onMinChange(event: Event): void {
    const value = +(event.target as HTMLInputElement).value;
    
    if (value <= this.currentMax) {
      this.currentMin = value;
      this.emitChange();
    }
  }

  onMaxChange(event: Event): void {
    const value = +(event.target as HTMLInputElement).value;
    
    if (value >= this.currentMin) {
      this.currentMax = value;
      this.emitChange();
    }
  }

  private emitChange(): void {
    const value: RangeValue = {
      min: this.currentMin,
      max: this.currentMax
    };
    this.onChange(value);
    this.rangeChange.emit(value);
  }

  getMinPercentage(): number {
    return ((this.currentMin - this.min) / (this.max - this.min)) * 100;
  }

  getMaxPercentage(): number {
    return ((this.currentMax - this.min) / (this.max - this.min)) * 100;
  }

  formatDisplayValue(value: number): string {
    if (this.formatValue) {
      return this.formatValue(value);
    }
    return value.toString();
  }
}