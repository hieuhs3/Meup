import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  // Khởi tạo ThemeService sớm để áp giao diện sáng/tối ngay khi vào app.
  private readonly theme = inject(ThemeService);
  protected readonly title = signal('frontend');
}
