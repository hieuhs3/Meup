# MeUp — Testing Guide

> xUnit (backend) · Karma + Jasmine (frontend) · 146 BE + 29 FE tests

---

## 1. Tổng quan

| Layer | Framework | Test count | DB cần |
|-------|-----------|-----------|--------|
| Backend | xUnit + WebApplicationFactory | 146 tests | Postgres port 5433 |
| Frontend | Karma + Jasmine | 29 tests | Không |

---

## 2. Backend Tests (xUnit)

### Chạy tests

```bash
# Bước 1: Khởi động Postgres (port 5433)
docker compose up -d

# Bước 2: Chạy tất cả tests
cd backend
dotnet test

# Chạy với output chi tiết
dotnet test --verbosity normal

# Chạy tests filter theo tên
dotnet test --filter "FullyQualifiedName~Finance"
```

### Cấu trúc test project

```
MeUp.Tests/
├── Unit/
│   └── TokenServiceTests.cs     # JWT + 2FA token generation/validation
├── Integration/
│   ├── AuthTests.cs              # Register, Login, Refresh, Logout
│   ├── ProfileTests.cs           # F0E: Profile, Avatar, Email change, Delete
│   ├── TwoFactorTests.cs         # F0E: 2FA setup, enable, login flow
│   ├── FinanceTests.cs           # F1: Transactions, Categories, Budget, Assets (G4)
│   ├── HealthTests.cs            # F2+G5: HealthLogs, Activities, BMI
│   ├── WorkTests.cs              # F3+G1+G3+G11: Tasks, Goals, Habits, Kanban
│   ├── JournalTests.cs           # F5+G2: Journal, Mood
│   ├── EventTests.cs             # F4: Calendar events
│   ├── NoteTests.cs              # G6: Notes, Tags, Backlinks
│   ├── CareerTests.cs            # G7: Skills, Certifications, Projects
│   ├── DocumentTests.cs          # G8: Upload, Download
│   └── GoalTests.cs              # G1: Goal tree, Rollup progress
└── TestBase.cs                   # WebApplicationFactory + test DB setup
```

### Phương pháp test

**Integration tests** sử dụng `WebApplicationFactory<Program>`:
- Tạo instance thật của API.
- Kết nối tới DB test (`meup_test`) — tách biệt với `meup` dev.
- Mỗi test class: `IClassFixture<WebApplicationFactory<Program>>`.
- Seed data độc lập, cleanup sau mỗi test.
- Mock external services: `IGoogleTokenValidator` (không gọi Google thật).

```csharp
// Pattern điển hình
public class FinanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FinanceTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTransaction_ShouldReturn201()
    {
        // Arrange
        var token = await LoginAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var req = new { type = "income", amount = 500000, date = "2026-08-01" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/finance/transactions", req);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

---

## 3. Test Coverage theo Feature

### Auth (F0 + F0E)

| Test | Kiểm tra |
|------|---------|
| Register | Đăng ký thành công, trùng email → 409 |
| Login | Đúng/sai mật khẩu, khóa sau 5 lần sai |
| Refresh | Token hợp lệ/hết hạn/đã dùng |
| Profile update | Round-trip toàn bộ trường (tiếng Việt, DateOnly, gender) |
| Avatar upload | Thành công, sai MIME, vượt 2MB |
| Change email | Thành công, sai mật khẩu, email đã dùng |
| Delete account | Thành công, sai mật khẩu |
| Google login | Mock validator: user mới tạo, user đã có liên kết |
| 2FA setup+enable | TOTP code đúng/sai |
| 2FA login | Bước 1 → twoFactorToken, Bước 2 → AuthResponse |
| Recovery codes | Dùng một lần, dùng lại → fail |

### Finance (F1 + G4)

| Test | Kiểm tra |
|------|---------|
| Seed categories | Danh mục mặc định tạo khi đăng ký |
| Create transaction | income/expense, validate amount > 0 |
| Category isolation | Danh mục của user khác → 404 |
| Category type mismatch | income transaction + expense category → 400 |
| Delete category | Transactions không bị xóa, chỉ gỡ liên kết |
| Balance calculation | Σincome - Σexpense đúng |
| Filter by date/type | Lọc chính xác |
| Pagination | Page/pageSize đúng |
| User isolation | Không truy cập được dữ liệu user khác |
| 401 | Không có token → từ chối |
| Assets (G4) | CRUD tài sản, net worth calculation |

### Health (F2 + G5)

| Test | Kiểm tra |
|------|---------|
| Upsert health log | Tạo mới và cập nhật cùng ngày |
| BMI calculation | Tính đúng với weight + height |
| Activity CRUD | Tạo/sửa/xóa hoạt động |

### Work (F3 + G1 + G3 + G11)

| Test | Kiểm tra |
|------|---------|
| Task CRUD | Tạo, hoàn thành, xóa |
| Task overdue | Task quá hạn được đánh dấu |
| Recurring task | Hoàn thành task lặp → sinh task mới |
| Sub-task | Tạo sub-task dưới task cha |
| Kanban status | Chuyển todo → in_progress → done |
| Goal tree | Tạo cây đa cấp, rollup tiến độ |
| Habit streak | Check habit, tính streak liên tiếp |
| Habit heatmap | 12 tuần completion data |

---

## 4. Frontend Tests (Karma + Jasmine)

### Chạy tests

```bash
cd frontend

# Chạy watch mode (mở browser)
npm test

# Chạy CI mode (headless, không cần user input)
npm run test:ci
# = ng test --watch=false --browsers=ChromeHeadless
```

> **Yêu cầu**: Chrome/Chromium phải cài sẵn. Nếu không tìm thấy, đặt biến `CHROME_BIN`:
> ```bash
> $env:CHROME_BIN = "C:\Program Files\Google\Chrome\Application\chrome.exe"
> npm run test:ci
> ```

### Test files

```
frontend/src/app/
├── core/services/
│   ├── auth.service.spec.ts      # 8 tests: login, logout, token storage
│   ├── finance.service.spec.ts   # 5 tests: mock HTTP calls
│   ├── search.service.spec.ts    # 3 tests: search query
│   └── theme.service.spec.ts     # 4 tests: dark mode toggle + persistence
├── features/
│   ├── auth/login.spec.ts        # 4 tests: form validation, submit
│   ├── today/today.spec.ts       # 3 tests: component renders, date change
│   └── journal/rich-editor.spec.ts  # 5 tests: bold, italic, heading, list
└── app.spec.ts                   # 1 test: root component
```

### Pattern điển hình

```typescript
// finance.service.spec.ts
describe('FinanceService', () => {
  let service: FinanceService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [FinanceService],
    });
    service = TestBed.inject(FinanceService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should get transactions', () => {
    const mockData = { items: [], total: 0 };

    service.getTransactions({}).subscribe(data => {
      expect(data).toEqual(mockData);
    });

    const req = httpMock.expectOne(`/api/finance/transactions`);
    expect(req.request.method).toBe('GET');
    req.flush(mockData);
  });

  afterEach(() => httpMock.verify());
});
```

---

## 5. Chiến lược test tổng thể

### Pyramid

```
         ▲  E2E (chưa có)
        / \
       /   \  Integration (146 BE tests)
      /     \
     /       \  Unit (29 FE + 10 BE unit)
    /---------\
```

### Nguyên tắc

1. **Integration tests ưu tiên ở BE**: Test qua HTTP endpoint thật (không mock Service/DB), đảm bảo toàn bộ stack hoạt động đúng.
2. **Unit tests ở FE**: Mock HTTP, test component logic và service methods.
3. **Test DB tách biệt**: `meup_test` — không ảnh hưởng DB dev `meup`.
4. **Data isolation**: Mỗi test tạo user riêng để tránh xung đột.
5. **CI-friendly**: Cả BE và FE tests đều chạy headless (không cần UI).

---

## 6. CI/CD Pipeline (GitHub Actions — nếu setup)

```yaml
# .github/workflows/test.yml
jobs:
  backend-test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_DB: meup_test
          POSTGRES_USER: meup
          POSTGRES_PASSWORD: meup_test_pass
        ports:
          - 5433:5432
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - run: cd backend && dotnet test

  frontend-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '20'
      - run: cd frontend && npm ci && npm run test:ci
```

---

## 7. Thêm test mới

### Backend Integration Test

```csharp
// 1. Tạo file MeUp.Tests/Integration/MyFeatureTests.cs
// 2. Kế thừa IClassFixture<WebApplicationFactory<Program>>
// 3. Login → lấy token → gọi endpoint
// 4. Assert response code + body
```

### Frontend Unit Test

```typescript
// 1. Tạo file feature.spec.ts cạnh file cần test
// 2. Configure TestBed với imports cần thiết
// 3. Mock HTTP với HttpClientTestingModule
// 4. Test từng method/behavior
```
