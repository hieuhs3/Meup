# MeUp — Database Schema

> PostgreSQL 16 · EF Core 9 · Tất cả bảng đều gắn `UserId` để cô lập dữ liệu

---

## 1. Tổng quan

```
Database: meup (dev) / ${POSTGRES_DB} (prod)
Collation: mặc định PostgreSQL
Extension: uuid-ossp (nếu cần; EF dùng C# Guid.NewGuid() để generate)
```

**Nguyên tắc thiết kế:**
- Mọi bảng nghiệp vụ có cột `UserId GUID NOT NULL` → foreign key tới `AspNetUsers(Id)`.
- `ON DELETE CASCADE`: xóa user → xóa toàn bộ dữ liệu liên quan.
- Ngày tháng dùng `DateOnly` (chỉ ngày, không giờ) hoặc `DateTimeOffset`/`DateTime UTC` tùy ngữ cảnh.
- `numeric(18,2)` cho tiền tệ; `numeric(5,2)` cho cân nặng, chiều cao.

---

## 2. Bảng Identity (ASP.NET Core Identity)

### AspNetUsers (ApplicationUser)

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | Guid |
| `UserName` | varchar(256) | = Email |
| `NormalizedUserName` | varchar(256) | UPPERCASE |
| `Email` | varchar(256) | |
| `NormalizedEmail` | varchar(256) | index |
| `EmailConfirmed` | boolean | |
| `PasswordHash` | text | bcrypt hash |
| `SecurityStamp` | text | đổi khi thay mật khẩu |
| `PhoneNumber` | text | tùy chọn |
| `TwoFactorEnabled` | boolean | |
| `LockoutEnd` | timestamptz | |
| `AccessFailedCount` | int | max 5 → lockout 15 phút |
| `DisplayName` | varchar(100) | |
| `IsLocked` | boolean | admin khóa |
| `CreatedAt` | timestamptz | |
| `DateOfBirth` | date | nullable |
| `Gender` | varchar(20) | male/female/other |
| `Bio` | varchar(500) | |
| `AvatarUrl` | varchar(256) | relative path |
| `TimeZone` | varchar(64) | IANA vd "Asia/Ho_Chi_Minh" |
| `Locale` | varchar(10) | vd "vi" |
| `DailyReportEnabled` | boolean | gửi email tổng kết 21:00 |
| `EncryptedAiApiKey` | text | BYO API key, encrypted |

### AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens

> Bảng Identity chuẩn — dùng cho role (Admin/User), Google external login, 2FA authenticator key.

---

## 3. Bảng Auth

### RefreshTokens

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK → AspNetUsers | CASCADE |
| `TokenHash` | varchar(128) NOT NULL | SHA-256 hash của token gốc |
| `ExpiresAt` | timestamptz | 7 ngày sau khi tạo |
| `CreatedAt` | timestamptz | |
| `RevokedAt` | timestamptz? | bị thu hồi khi logout/rotate |

**Index**: `TokenHash` (tìm nhanh khi validate)

---

## 4. Bảng Tài chính (F1 + G4)

### Categories

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Name` | varchar(50) NOT NULL | |
| `Type` | varchar(10) NOT NULL | "income" \| "expense" |
| `Color` | varchar(7) | hex color vd "#FF5733" |
| `IsDefault` | boolean | seed mặc định khi đăng ký |

**Index**: `(UserId, Type)`

### Transactions

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `CategoryId` | uuid FK → Categories | SET NULL khi xóa category |
| `Type` | varchar(10) NOT NULL | "income" \| "expense" |
| `Amount` | numeric(18,2) NOT NULL | > 0 |
| `Date` | date NOT NULL | ngày giao dịch |
| `Note` | varchar(500) | |
| `CreatedAt` | timestamptz | |

**Index**: `(UserId, Date)`

### Budgets (A1)

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `CategoryId` | uuid FK → Categories | CASCADE |
| `Amount` | numeric(18,2) NOT NULL | ngân sách/tháng |
| `Month` | varchar(7) | "YYYY-MM" |

**Index**: `(UserId, CategoryId)` UNIQUE

### Assets (G4)

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Name` | varchar(150) NOT NULL | |
| `Type` | varchar(10) | cash/bank/stock/crypto/gold/other |
| `Value` | numeric(18,2) | giá trị hiện tại |
| `Note` | varchar(500) | |
| `UpdatedAt` | timestamptz | |

**Index**: `(UserId, Type)`

---

## 5. Bảng Sức khỏe (F2 + G5)

### HealthLogs

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Date` | date NOT NULL | mỗi ngày 1 bản ghi |
| `Weight` | numeric(5,2) | kg |
| `HeightCm` | numeric(5,2) | cm, dùng tính BMI |
| `SleepHours` | numeric(4,1) | giờ ngủ |
| `WaterMl` | int | ml |
| `WorkoutMinutes` | int | phút |
| `Note` | varchar(500) | |
| `CreatedAt` | timestamptz | |
| `UpdatedAt` | timestamptz | |

**Index**: `(UserId, Date)` UNIQUE — mỗi ngày 1 bản ghi

### Activities (G5)

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Date` | date NOT NULL | |
| `Type` | varchar(10) | running/walking/gym/swimming/cycling/other |
| `DurationMin` | int | phút |
| `Calories` | int? | kcal |
| `Note` | varchar(500) | |
| `CreatedAt` | timestamptz | |

**Index**: `(UserId, Date)`

---

## 6. Bảng Công việc & Mục tiêu (F3 + G1 + G3 + G11)

### Tasks (TaskItem)

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `GoalId` | uuid FK → Goals | CASCADE (task thuộc goal) |
| `ParentTaskId` | uuid FK → Tasks | CASCADE (sub-task) |
| `Title` | varchar(200) NOT NULL | |
| `IsDone` | boolean | |
| `DueDate` | date? | hạn chót |
| `CompletedAt` | timestamptz? | |
| `Recurrence` | varchar(10) | none/daily/weekly/monthly |
| `Priority` | varchar(10) | low/medium/high/critical |
| `Status` | varchar(12) | todo/in_progress/review/done/cancelled |
| `CreatedAt` | timestamptz | |

**Index**: `(UserId, IsDone)`, `GoalId`, `ParentTaskId`

### Goals (G1)

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `ParentGoalId` | uuid FK → Goals | CASCADE (cây đệ quy) |
| `Name` | varchar(150) NOT NULL | |
| `Description` | varchar(1000) | |
| `Level` | varchar(10) | life/year/quarter/month/week |
| `Status` | varchar(12) | draft/active/completed/cancelled/archived |
| `Progress` | int | 0–100, server tính rollup |
| `TargetDate` | date? | |
| `CreatedAt` | timestamptz | |

**Index**: `UserId`, `(UserId, ParentGoalId)`

### Habits

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Name` | varchar(150) NOT NULL | |
| `Frequency` | varchar(10) | daily/weekly |
| `TargetPerWeek` | int? | mục tiêu số lần/tuần |
| `CreatedAt` | timestamptz | |

**Index**: `UserId`

### HabitChecks (G3)

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `HabitId` | uuid FK → Habits | CASCADE |
| `UserId` | uuid | denormalized để query nhanh |
| `Date` | date NOT NULL | ngày check |

**Index**: `(HabitId, Date)` UNIQUE

---

## 7. Bảng Nhật ký (F5 + G2)

### JournalEntries

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Date` | date NOT NULL | ngày nhật ký |
| `Title` | varchar(200) | tiêu đề (tùy chọn) |
| `Content` | text | rich-text HTML |
| `Mood` | varchar(20) | happy/neutral/sad/stressed/energetic |
| `CreatedAt` | timestamptz | |
| `UpdatedAt` | timestamptz | |

**Index**: `(UserId, Date)`

---

## 8. Bảng Lịch trình (F4)

### CalendarEvents

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Title` | varchar(200) NOT NULL | |
| `Date` | date NOT NULL | |
| `StartTime` | time? | |
| `EndTime` | time? | |
| `Location` | varchar(200) | |
| `Note` | varchar(1000) | |
| `CreatedAt` | timestamptz | |

**Index**: `(UserId, Date)`

---

## 9. Bảng Thuốc (A2)

### Medications

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Name` | varchar(150) NOT NULL | |
| `Dosage` | varchar(100) | liều lượng |
| `Frequency` | varchar(50) | lịch uống |
| `ReminderTime` | time? | giờ nhắc |
| `Note` | varchar(500) | |
| `IsActive` | boolean | |
| `CreatedAt` | timestamptz | |

**Index**: `UserId`

### MedicationIntakes

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `MedicationId` | uuid FK → Medications | CASCADE |
| `Date` | date NOT NULL | ngày uống |
| `TakenAt` | timestamptz | thời điểm ghi nhận |

**Index**: `(MedicationId, Date)` UNIQUE

---

## 10. Bảng Ghi chú (G6)

### Notes

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Title` | varchar(200) | |
| `Content` | text | hỗ trợ `[[backlink]]` syntax |
| `Category` | varchar(50) | phân loại |
| `Tags` | text[] | mảng tag PostgreSQL |
| `CreatedAt` | timestamptz | |
| `UpdatedAt` | timestamptz | |

**Index**: `UserId`

---

## 11. Bảng Sự nghiệp (G7)

### Skills

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Name` | varchar(100) NOT NULL | |
| `Category` | varchar(50) | |
| `Level` | int | 1–5 |
| `Notes` | text | |

### Certifications

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Name` | varchar(150) NOT NULL | |
| `Issuer` | varchar(100) | |
| `IssuedDate` | date? | |
| `ExpiryDate` | date? | |
| `CredentialId` | varchar(100) | |

### CareerProjects

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Name` | varchar(150) NOT NULL | |
| `Role` | varchar(100) | |
| `Description` | varchar(2000) | |
| `StartDate` | date? | |
| `EndDate` | date? | |
| `Technologies` | text[] | |

---

## 12. Bảng Tài liệu (G8)

### Documents

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `FileName` | varchar(260) NOT NULL | tên file gốc |
| `ContentType` | varchar(150) | MIME type |
| `StorageKey` | varchar(260) NOT NULL | key trong IFileStorage |
| `Category` | varchar(15) | identity/finance/health/education/work/other |
| `SizeBytes` | long | |
| `UploadedAt` | timestamptz | |

**Index**: `(UserId, Category)`

---

## 13. Bảng Thông báo

### Notifications

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `Type` | varchar(20) | loại thông báo |
| `Title` | varchar(200) | |
| `Message` | varchar(1000) | |
| `Link` | varchar(256) | đường dẫn tới màn hình liên quan |
| `IsRead` | boolean | |
| `DedupKey` | varchar(100) | tránh tạo thông báo trùng |
| `CreatedAt` | timestamptz | |

**Index**: `(UserId, CreatedAt)`, `(UserId, DedupKey)`

---

## 14. Bảng WeeklyInsights (AI Cache)

### WeeklyInsights

| Cột | Type | Ghi chú |
|-----|------|---------|
| `Id` | uuid PK | |
| `UserId` | uuid FK | CASCADE |
| `WeekFrom` | date NOT NULL | |
| `WeekTo` | date NOT NULL | |
| `Content` | text | kết quả từ Claude API |
| `GeneratedAt` | timestamptz | |

**Index**: `(UserId, WeekFrom, WeekTo)` UNIQUE — 1 insight/tuần/user

---

## 15. Quan hệ bảng (tóm tắt)

```
AspNetUsers ──< RefreshTokens         (1:N)
AspNetUsers ──< Categories             (1:N)
AspNetUsers ──< Transactions           (1:N)
Categories  ──< Transactions           (1:N, SET NULL)
AspNetUsers ──< Budgets                (1:N)
Categories  ──< Budgets                (1:N)
AspNetUsers ──< Assets                 (1:N)
AspNetUsers ──< HealthLogs             (1:N, UNIQUE Date)
AspNetUsers ──< Activities             (1:N)
AspNetUsers ──< Goals                  (1:N)
Goals       ──< Goals (Parent)         (self-ref 1:N, CASCADE)
AspNetUsers ──< Tasks                  (1:N)
Goals       ──< Tasks                  (1:N, CASCADE)
Tasks       ──< Tasks (SubTasks)       (self-ref 1:N, CASCADE)
AspNetUsers ──< Habits                 (1:N)
Habits      ──< HabitChecks            (1:N, UNIQUE Date)
AspNetUsers ──< JournalEntries         (1:N)
AspNetUsers ──< CalendarEvents         (1:N)
AspNetUsers ──< Medications            (1:N)
Medications ──< MedicationIntakes      (1:N, UNIQUE Date)
AspNetUsers ──< Notes                  (1:N)
AspNetUsers ──< Skills                 (1:N)
AspNetUsers ──< Certifications         (1:N)
AspNetUsers ──< CareerProjects         (1:N)
AspNetUsers ──< Documents              (1:N)
AspNetUsers ──< Notifications          (1:N)
AspNetUsers ──< WeeklyInsights         (1:N, UNIQUE Week)
```
