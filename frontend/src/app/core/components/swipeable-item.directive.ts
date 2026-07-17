import { Directive, ElementRef, HostListener, input, output, inject, OnDestroy } from '@angular/core';

/**
 * SwipeableItem directive — gắn vào một list item để bật swipe-to-reveal.
 * Kéo sang trái → hiện nút xóa/sửa bên phải.
 * Kéo sang phải hoặc tap ngoài → đóng lại.
 *
 * Cách dùng:
 *   <div appSwipeableItem (swipeLeft)="onDelete(item)">...</div>
 */
@Directive({
  selector: '[appSwipeableItem]',
  host: {
    'class': 'swipeable-item',
    '[style.transform]': 'translateX()',
    '[style.transition]': 'transitioning ? "transform 0.25s ease" : "none"',
  },
})
export class SwipeableItem implements OnDestroy {
  /** Khoảng cách tối đa swipe (px) — bằng với chiều rộng action buttons */
  readonly maxSwipe = input<number>(88);

  readonly swipeLeft = output<void>();

  private readonly el = inject(ElementRef<HTMLElement>);

  private startX = 0;
  private currentX = 0;
  private isDragging = false;
  transitioning = false;

  translateX(): string {
    return `translateX(${this.currentX}px)`;
  }

  @HostListener('touchstart', ['$event'])
  onTouchStart(e: TouchEvent): void {
    this.startX = e.touches[0].clientX;
    this.isDragging = true;
    this.transitioning = false;
  }

  @HostListener('touchmove', ['$event'])
  onTouchMove(e: TouchEvent): void {
    if (!this.isDragging) return;
    const dx = e.touches[0].clientX - this.startX;
    // Chỉ cho swipe sang trái (dx âm), max = -maxSwipe
    this.currentX = Math.max(-this.maxSwipe(), Math.min(0, dx));
  }

  @HostListener('touchend')
  onTouchEnd(): void {
    this.isDragging = false;
    this.transitioning = true;
    // Nếu swipe quá 40% threshold → snap sang trạng thái mở
    if (this.currentX < -(this.maxSwipe() * 0.4)) {
      this.currentX = -this.maxSwipe();
    } else {
      this.currentX = 0;
    }
  }

  /** Đóng lại từ bên ngoài (ví dụ khi item khác được swipe) */
  close(): void {
    this.transitioning = true;
    this.currentX = 0;
  }

  ngOnDestroy(): void {
    // cleanup nếu cần
  }
}
