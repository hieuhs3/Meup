# PersonalOS

## Personal Life Management Platform

Version: 1.0

Author: Hieu Ho Sy

---

# 1. Vision

## 1.1 Mục tiêu

PersonalOS là một nền tảng quản lý toàn diện cuộc sống cá nhân.

Mục tiêu của hệ thống là trở thành:

> Một bộ não số (Digital Brain) giúp người dùng quản lý công việc, mục tiêu, kiến thức, tài chính, sức khỏe và hỗ trợ ra quyết định bằng AI.

Thay vì sử dụng nhiều ứng dụng khác nhau như:

* Notion
* Google Calendar
* Todoist
* Obsidian
* Money Lover
* Google Keep

Người dùng chỉ cần sử dụng một hệ thống duy nhất.

---

# 2. Business Goals

## Ngắn hạn

Cho phép người dùng:

* Quản lý công việc
* Quản lý mục tiêu
* Ghi chú
* Nhật ký

## Trung hạn

Cho phép:

* Theo dõi tài chính
* Theo dõi sức khỏe
* Quản lý kỹ năng

## Dài hạn

Xây dựng AI Personal Assistant có khả năng:

* Hiểu lịch sử cá nhân
* Hiểu hành vi
* Đưa ra lời khuyên
* Lập kế hoạch tương lai

---

# 3. Functional Modules

## 3.1 Identity Module

### Chức năng

* Đăng ký
* Đăng nhập
* Quên mật khẩu
* Đổi mật khẩu
* Refresh Token
* MFA

### Thông tin người dùng

```text
User
├── Full Name
├── Birthday
├── Gender
├── Phone
├── Email
├── Avatar
├── Occupation
├── Address
├── Timezone
└── Language
```

---

## 3.2 Goal Management Module

Mục tiêu được tổ chức theo nhiều cấp.

```text
Life Goal
    │
    ├── Year Goal
    │
    ├── Quarter Goal
    │
    ├── Month Goal
    │
    └── Weekly Goal
```

Ví dụ:

```text
Become Solution Architect

├── Learn Cloud
├── Learn System Design
├── Get AWS Certificate
└── Build PersonalOS
```

### Chức năng

* Tạo Goal
* Chỉnh sửa Goal
* Archive Goal
* Theo dõi tiến độ
* Goal Dashboard

### Trạng thái

```text
Draft
Active
Completed
Cancelled
Archived
```

---

## 3.3 Task Management

### Chức năng

* Todo
* Checklist
* Kanban Board
* Sprint
* Reminder

### Priority

```text
Low
Medium
High
Critical
```

### Status

```text
Todo
InProgress
Review
Done
Cancelled
```

### Relationships

```text
Goal
   |
   └── Tasks
```

Ví dụ:

```text
Goal:
Learn Docker

Tasks:
- Learn Docker Compose
- Learn Dockerfile
- Learn Multi Stage Build
```

---

## 3.4 Habit Tracking

Theo dõi thói quen.

Ví dụ:

```text
Read Book
Exercise
Meditation
Study English
```

### Thông tin

```text
Habit
├── Name
├── Frequency
├── Target
├── Current Streak
├── Best Streak
└── Completion Rate
```

### Dashboard

Hiển thị:

* Streak
* Completion %
* Heatmap

---

## 3.5 Knowledge Management

Tương tự Obsidian.

### Chức năng

* Notes
* Tags
* Categories
* Backlinks
* Search

### Ví dụ

```text
.NET

├── LINQ
├── EF Core
├── ASP.NET
└── Microservices
```

---

## 3.6 Journal Module

### Daily Journal

```text
Today I completed:

- CRS booking API
- Docker deployment
- Oracle optimization
```

### Mood Tracking

```text
Excellent
Good
Normal
Bad
Terrible
```

### Dashboard

* Mood Trend
* Weekly Summary
* Monthly Summary

---

## 3.7 Finance Module

### Income

```text
Salary
Bonus
Freelance
Investment
```

### Expense

```text
Food
Transport
Entertainment
Shopping
Education
```

### Asset

```text
Cash
Bank
Stock
Crypto
Gold
```

### Reports

* Monthly Expense
* Saving Rate
* Net Worth
* Cash Flow

---

## 3.8 Health Module

### Physical Health

```text
Weight
Height
BMI
Body Fat
Calories
```

### Activities

```text
Running
Walking
Gym
Swimming
Cycling
```

### Reports

* Weight Trend
* Activity Trend
* Calories Report

---

## 3.9 Career Module

Dành riêng cho việc phát triển sự nghiệp.

### Skills

```text
.NET
Java
Oracle
Docker
Kubernetes
System Design
```

### Certifications

```text
AWS
Azure
Oracle
Microsoft
```

### Projects

```text
Synapse
PersonalOS
SmartHotel
DNI
```

### Career Goals

```text
Junior
Mid-Level
Senior
Architect
```

---

## 3.10 Document Module

Lưu trữ tài liệu cá nhân.

### Categories

```text
CV
Certificate
Contract
Invoice
Personal Documents
```

### Storage

* Local Storage
* MinIO
* AWS S3

---

## 3.11 Notification Module

### Notification Types

```text
Task Reminder
Goal Reminder
Habit Reminder
System Notification
```

### Channels

```text
Email
Push Notification
Telegram
SMS
```

---

## 3.12 AI Assistant Module

Module quan trọng nhất.

### Chức năng

#### Smart Search

Ví dụ:

```text
Tôi đã học gì về Docker tháng trước?
```

#### Daily Summary

```text
Hôm nay bạn đã:

- Hoàn thành 8 tasks
- Viết 3 notes
- Đạt streak 15 ngày
```

#### Goal Analysis

```text
Bạn đang chậm tiến độ Goal AWS Certificate 20%.
```

#### Planning

```text
Lập kế hoạch học Kubernetes trong 30 ngày.
```

---

# 4. Non Functional Requirements

## Performance

API Response:

```text
< 300 ms
```

Dashboard:

```text
< 2 seconds
```

---

## Availability

```text
99.9%
```

---

## Security

Authentication:

```text
JWT
Refresh Token
```

Authorization:

```text
RBAC
```

Encryption:

```text
AES
```

Password:

```text
BCrypt
```

---

# 5. Architecture

## Style

```text
Modular Monolith
```

Giai đoạn đầu không sử dụng Microservice.

---

## Clean Architecture

```text
Presentation
     |
Application
     |
Domain
     |
Infrastructure
```

---

# 6. Database Design

## Core Tables

### Users

```sql
users
```

### Goals

```sql
goals
```

### Tasks

```sql
tasks
```

### Habits

```sql
habits
```

### HabitLogs

```sql
habit_logs
```

### Notes

```sql
notes
```

### Journals

```sql
journals
```

### Transactions

```sql
transactions
```

### Assets

```sql
assets
```

### HealthRecords

```sql
health_records
```

### Skills

```sql
skills
```

### UserSkills

```sql
user_skills
```

### Documents

```sql
documents
```

---

# 7. AI Architecture

## Data Sources

```text
Goals
Tasks
Notes
Journals
Finance
Health
```

## Embedding Flow

```text
User Data
    |
Chunking
    |
Embedding
    |
Vector Database
    |
AI Retrieval
```

Vector Database:

```text
Qdrant
PgVector
```

---

# 8. Development Roadmap

## Phase 1 (1-2 tháng)

* Identity
* Goal
* Task
* Journal

## Phase 2 (1 tháng)

* Habit
* Knowledge

## Phase 3 (1 tháng)

* Finance

## Phase 4 (1 tháng)

* Health

## Phase 5 (2 tháng)

* AI Assistant
* RAG
* Recommendation Engine

## Phase 6

* Mobile App
* Offline Sync
* Multi Device Support

---

# 9. Long-Term Vision

PersonalOS sẽ trở thành:

> Một hệ điều hành cá nhân thông minh, lưu trữ toàn bộ kiến thức, ký ức, mục tiêu và hỗ trợ người dùng ra quyết định bằng AI.

Mọi dữ liệu cuộc sống đều được kết nối và phân tích để giúp người dùng phát triển bản thân một cách liên tục.
