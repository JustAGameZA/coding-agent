import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App, NoopAnimationsModule],
      providers: [
        provideHttpClient(),
        provideRouter([])
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render toolbar title when authenticated', () => {
    const fixture = TestBed.createComponent(App);
    // Mock authenticated state
    fixture.componentInstance['authService'].setToken('mock-token');
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const titleEl = compiled.querySelector('.toolbar-title');
    expect(titleEl?.textContent?.trim()).toBe('Coding Agent Dashboard');
  });
  
  it('should not render toolbar when not authenticated', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const toolbar = compiled.querySelector('.app-toolbar');
    expect(toolbar).toBeFalsy();
  });
});
