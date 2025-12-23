# Podium Design System Guide

## Table of Contents
1. [Color System](#color-system)
2. [How to Change Themes](#how-to-change-themes)
3. [Component Patterns](#component-patterns)
4. [Usage Examples](#usage-examples)
5. [Removing Material Icons](#removing-material-icons)

---

## Color System

### Semantic Color Tokens
The design system uses semantic tokens that you can change globally in `tailwind.config.js`:

```javascript
// Primary Brand Color (Blue by default)
primary: { 50-950 }  // Use: bg-primary-500, text-primary-600, etc.

// Secondary Accent (Amber/Gold)
secondary: { 50-950 }  // Use: bg-secondary-500, text-secondary-600, etc.

// Status Colors
success: { 50-950 }   // Green
warning: { 50-950 }   // Orange
danger: { 50-950 }    // Red
info: { 50-950 }      // Cyan

// Layout Colors (Semantic)
background: { DEFAULT, dark, darker }  // Page backgrounds
surface: { DEFAULT, hover, active }    // Cards, panels
border: { DEFAULT, light, dark }       // Borders
text: { primary, secondary, tertiary, disabled, inverse }  // Text colors
```

### Usage in HTML
```html
<!-- Instead of hardcoded colors -->
<div class="bg-blue-500">  <!-- ❌ Don't do this -->

<!-- Use semantic tokens -->
<div class="bg-primary-500">  <!-- ✅ Do this -->
<div class="bg-surface border border-border">  <!-- ✅ Perfect -->
<p class="text-text-secondary">  <!-- ✅ Great -->
```

---

## How to Change Themes

### Option 1: Change in tailwind.config.js (Recommended)
To change your entire color scheme, edit the color values in `tailwind.config.js`:

```javascript
// Current Blue Theme
primary: {
  500: '#3b82f6',  // Main brand color
  600: '#2563eb',  // Hover state
}

// Change to Purple Theme (HBCU-inspired)
primary: {
  500: '#7c3aed',  // Purple
  600: '#6d28d9',  // Darker purple
}

// Change to Maroon Theme (HBCU-inspired)
primary: {
  500: '#881337',  // Maroon
  600: '#7f1d1d',  // Darker maroon
}
```

### Option 2: Use CSS Custom Properties (Advanced)
For runtime theme switching, use the CSS variables in `styles.scss`:

```scss
:root {
  --color-primary: 59 130 246;  // Blue theme
}

[data-theme="hbcu-purple"] {
  --color-primary: 124 58 237;  // Purple theme
}

[data-theme="hbcu-maroon"] {
  --color-primary: 136 19 55;   // Maroon theme
}
```

Then in your TypeScript:
```typescript
// Switch themes dynamically
document.documentElement.setAttribute('data-theme', 'hbcu-purple');
```

### Pre-configured HBCU Theme Examples

#### Purple & Gold (Howard, Prairie View, etc.)
```javascript
primary: {
  500: '#7c3aed',  // Purple-600
  600: '#6d28d9',
}
secondary: {
  500: '#eab308',  // Yellow-500
  600: '#ca8a04',
}
```

#### Maroon & Gold (Morehouse, Tuskegee, etc.)
```javascript
primary: {
  500: '#881337',  // Rose-900
  600: '#7f1d1d',  // Red-900
}
secondary: {
  500: '#eab308',  // Yellow-500
  600: '#ca8a04',
}
```

#### Navy & Gold (Hampton, Jackson State, etc.)
```javascript
primary: {
  500: '#1e3a8a',  // Blue-900
  600: '#1e40af',  // Blue-800
}
secondary: {
  500: '#eab308',  // Yellow-500
  600: '#ca8a04',
}
```

---

## Component Patterns

### Buttons
```html
<!-- Primary Button -->
<button class="btn-primary">
  Click Me
</button>

<!-- Secondary Button -->
<button class="btn-secondary">
  Secondary Action
</button>

<!-- Outline Button -->
<button class="btn-outline">
  Cancel
</button>

<!-- Ghost Button -->
<button class="btn-ghost">
  Text Button
</button>

<!-- Danger Button -->
<button class="btn-danger">
  Delete
</button>

<!-- Sizes -->
<button class="btn-primary btn-sm">Small</button>
<button class="btn-primary">Default</button>
<button class="btn-primary btn-lg">Large</button>

<!-- With Loading State -->
<button class="btn-primary" [disabled]="isLoading">
  <svg *ngIf="isLoading" class="spinner h-5 w-5 mr-2">...</svg>
  {{ isLoading ? 'Submitting...' : 'Submit' }}
</button>
```

### Cards
```html
<!-- Basic Card -->
<div class="card">
  <div class="card-header">
    <h3 class="font-semibold">Card Title</h3>
  </div>
  <div class="card-body">
    <p>Card content goes here.</p>
  </div>
  <div class="card-footer">
    <button class="btn-primary">Action</button>
  </div>
</div>

<!-- Hoverable Card -->
<div class="card-hover">
  <div class="card-body">
    <p>This card lifts on hover</p>
  </div>
</div>
```

### Badges
```html
<span class="badge-primary">Active</span>
<span class="badge-secondary">Premium</span>
<span class="badge-success">Approved</span>
<span class="badge-warning">Pending</span>
<span class="badge-danger">Rejected</span>
<span class="badge-info">New</span>
```

### Alerts
```html
<div class="alert-success">
  <svg class="w-5 h-5 flex-shrink-0">...</svg>
  <div>
    <h4 class="font-bold">Success!</h4>
    <p class="text-sm">Your changes have been saved.</p>
  </div>
</div>

<div class="alert-warning">...</div>
<div class="alert-danger">...</div>
<div class="alert-info">...</div>
```

### Forms
```html
<div class="space-y-4">
  <!-- Input Field -->
  <div>
    <label class="label" for="email">Email</label>
    <input 
      type="email" 
      id="email" 
      class="input" 
      placeholder="Enter your email"
    />
  </div>

  <!-- Input with Error -->
  <div>
    <label class="label" for="password">Password</label>
    <input 
      type="password" 
      id="password" 
      class="input-error" 
      placeholder="Enter password"
    />
    <p class="text-danger-600 text-sm mt-1">Password is required</p>
  </div>

  <!-- Input with Success -->
  <div>
    <label class="label" for="username">Username</label>
    <input 
      type="text" 
      id="username" 
      class="input-success" 
      placeholder="Enter username"
    />
    <p class="text-success-600 text-sm mt-1">Username available!</p>
  </div>
</div>
```

### Dashboard Stat Cards
```html
<div class="stat-card border-success-500">
  <div class="flex items-center justify-between">
    <div>
      <div class="text-xs font-bold text-success-600 uppercase mb-1">
        Scholarship Offers
      </div>
      <div class="text-2xl font-bold text-text-primary">24</div>
    </div>
    <div class="text-success-500">
      <svg class="w-8 h-8">...</svg>
    </div>
  </div>
</div>
```

---

## Usage Examples

### Before and After Component Updates

#### Student Dashboard - BEFORE
```html
<div class="bg-white rounded-lg shadow border-l-4 border-green-500 p-4">
```

#### Student Dashboard - AFTER
```html
<div class="stat-card border-success-500">
```

#### Guardian Dashboard - BEFORE
```html
<button (click)="openApprovalModal(approval)" 
  class="flex-shrink-0 px-5 py-2.5 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg">
```

#### Guardian Dashboard - AFTER
```html
<button (click)="openApprovalModal(approval)" class="btn-primary">
```

#### Login Form - BEFORE
```html
<input class="block w-full pl-10 pr-3 py-2 border border-border rounded-md">
```

#### Login Form - AFTER
```html
<input class="input pl-10">
```

---

## Removing Material Icons

### Current Issues Found
Your components reference Material Icons but it's not configured. Here are the fixes:

#### Replace in header.html
```html
<!-- BEFORE -->
<span class="material-icons">menu</span>
<span class="material-icons">notifications</span>
<span class="material-icons text-sm">person</span>

<!-- AFTER - Use Heroicons or inline SVG -->
<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
    d="M4 6h16M4 12h16M4 18h16" />
</svg>
```

#### Replace in sidebar.html
```html
<!-- BEFORE -->
<span class="material-icons text-text-secondary group-hover:text-primary">dashboard</span>

<!-- AFTER -->
<svg class="w-5 h-5 text-text-secondary group-hover:text-primary" 
     fill="none" stroke="currentColor" viewBox="0 0 24 24">
  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
    d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
</svg>
```

#### Replace in landing.component.html
```html
<!-- BEFORE -->
<span class="material-icons text-primary text-3xl">{{ feature.icon }}</span>

<!-- AFTER -->
<svg class="w-8 h-8 text-primary-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
  <!-- Different icon SVG based on feature -->
</svg>
```

### Icon Libraries to Consider
1. **Heroicons** (Recommended - Tailwind's official icons)
   - Free, MIT license
   - Perfect match for Tailwind CSS
   - Available as Angular components

2. **Lucide Icons**
   - Modern, clean design
   - Tree-shakeable
   - Angular package available

3. **Inline SVG**
   - No dependencies
   - Full control
   - Best performance

---

## Quick Reference

### Most Common Classes

```html
<!-- Buttons -->
btn-primary, btn-secondary, btn-outline, btn-ghost, btn-danger

<!-- Cards -->
card, card-hover, card-header, card-body, card-footer

<!-- Badges -->
badge-primary, badge-success, badge-warning, badge-danger

<!-- Alerts -->
alert-success, alert-warning, alert-danger, alert-info

<!-- Forms -->
input, input-error, input-success, label

<!-- Layout -->
bg-background, bg-surface, border-border
text-text-primary, text-text-secondary

<!-- Stats -->
stat-card
```

### Color Usage Guide
- **Buttons**: `btn-primary`, `btn-secondary`, `btn-danger`
- **Status**: `badge-success`, `badge-warning`, `badge-danger`
- **Backgrounds**: `bg-surface`, `bg-background`
- **Borders**: `border-border`, `border-primary-500`
- **Text**: `text-text-primary`, `text-text-secondary`

---

## Next Steps

1. ✅ Replace `tailwind.config.js` with the new version
2. ✅ Replace `styles.scss` with the new version
3. 🔲 Remove Material Icons from all components
4. 🔲 Update components to use semantic tokens
5. 🔲 Apply component pattern classes
6. 🔲 Test theme switching (optional)

Need help with any specific component updates? Let me know!