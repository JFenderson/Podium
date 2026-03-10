import { defineConfig } from 'vitest/config';
import { resolve } from 'path';
import { readFileSync } from 'fs';
import swc from 'unplugin-swc';

// Inline Angular templateUrl and styleUrls for Vitest
function angularInlinePlugin() {
  return {
    name: 'angular-inline-resources',
    enforce: 'pre' as const,
    transform(code: string, id: string) {
      if (!id.endsWith('.ts') || id.includes('node_modules') || id.includes('.spec.ts')) {
        return null;
      }

      // Only process files with templateUrl or styleUrls
      if (!code.includes('templateUrl') && !code.includes('styleUrls')) {
        return null;
      }

      const dir = resolve(id, '..');
      let transformed = code;

      // Replace templateUrl: './foo.component.html' with template: '<content>'
      transformed = transformed.replace(
        /templateUrl\s*:\s*['"`]([^'"`]+)['"`]/g,
        (_, templatePath) => {
          const absPath = resolve(dir, templatePath);
          try {
            const content = readFileSync(absPath, 'utf-8')
              .replace(/\\/g, '\\\\')
              .replace(/`/g, '\\`')
              .replace(/\$\{/g, '\\${');
            return `template: \`${content}\``;
          } catch {
            return `template: ''`;
          }
        }
      );

      // Replace styleUrls: ['./foo.component.scss'] with styles: ['<content>']
      transformed = transformed.replace(
        /styleUrls\s*:\s*\[([^\]]*)\]/g,
        (_, urlList) => {
          const urls = urlList.match(/['"`]([^'"`]+)['"`]/g) || [];
          const styles = urls.map((u: string) => {
            const stylePath = u.replace(/['"`]/g, '');
            const absPath = resolve(dir, stylePath);
            try {
              const content = readFileSync(absPath, 'utf-8')
                .replace(/\\/g, '\\\\')
                .replace(/`/g, '\\`')
                .replace(/\$\{/g, '\\${');
              return `\`${content}\``;
            } catch {
              return '``';
            }
          });
          return `styles: [${styles.join(', ')}]`;
        }
      );

      // Also handle styleUrl (singular) for Angular 17+
      transformed = transformed.replace(
        /styleUrl\s*:\s*['"`]([^'"`]+)['"`]/g,
        (_, stylePath) => {
          const absPath = resolve(dir, stylePath);
          try {
            const content = readFileSync(absPath, 'utf-8')
              .replace(/\\/g, '\\\\')
              .replace(/`/g, '\\`')
              .replace(/\$\{/g, '\\${');
            return `styles: [\`${content}\`]`;
          } catch {
            return `styles: ['']`;
          }
        }
      );

      return { code: transformed, map: null };
    },
  };
}

export default defineConfig({
  plugins: [
    angularInlinePlugin(),
    swc.vite({
      jsc: {
        parser: { syntax: 'typescript', decorators: true },
        transform: { legacyDecorator: true, decoratorMetadata: true },
        target: 'es2022',
      },
    }),
  ],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['src/test-setup.ts'],
    include: ['src/**/*.spec.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html', 'lcov', 'json'],
      exclude: [
        'node_modules/',
        'src/test-setup.ts',
        '**/*.spec.ts',
        '**/*.config.ts',
        '**/main.ts',
        '**/*.d.ts'
      ],
      all: true,
      lines: 70,
      functions: 70,
      branches: 70,
      statements: 70
    }
  },
  resolve: {
    mainFields: ['module'],
    dedupe: [
      '@angular/animations',
      '@angular/common',
      '@angular/compiler',
      '@angular/core',
      '@angular/forms',
      '@angular/platform-browser',
      '@angular/platform-browser-dynamic',
      '@angular/router',
      'rxjs',
    ],
    alias: {
      '@': resolve(__dirname, './src'),
      '@app': resolve(__dirname, './src/app'),
      '@core': resolve(__dirname, './src/app/core'),
      '@shared': resolve(__dirname, './src/app/shared'),
      '@features': resolve(__dirname, './src/app/features')
    }
  }
});
