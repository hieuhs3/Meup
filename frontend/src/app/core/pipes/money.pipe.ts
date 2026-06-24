import { Pipe, PipeTransform } from '@angular/core';

/**
 * Định dạng tiền tệ VND theo chuẩn Việt Nam: phân tách nghìn bằng dấu "." (vd 1.000.000 ₫).
 * Dùng chung toàn hệ thống để đồng bộ: {{ amount | money }}.
 */
@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  private static readonly fmt = new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  });

  transform(value: number | null | undefined): string {
    if (value == null || isNaN(value)) return '—';
    return MoneyPipe.fmt.format(value);
  }
}
