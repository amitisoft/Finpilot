# 🚀 Personal Finance Tracker — Global Project Rules

## 1. Architecture & Tech Stack Standards

### Frontend
- **Framework:** React 18+ with TypeScript (strict mode enabled)
- **State Management:** TanStack Query for server state, Zustand for client state
- **Styling:** Tailwind CSS with CSS variables for theming
- **Forms:** React Hook Form + Zod for validation
- **Charts:** Recharts or Chart.js with responsive containers
- **Build Tool:** Vite for fast development and optimized builds

### Backend
- **Primary Stack:** ASP.NET Core 8+ with Entity Framework Core OR Java Spring Boot 3+
- **Database:** PostgreSQL 15+ with proper indexing
- **Authentication:** JWT with refresh tokens, bcrypt/Argon2 for password hashing
- **API Design:** RESTful with consistent response envelopes (`{ success, data, message, errors }`)
- **Documentation:** Swagger/OpenAPI with XML comments

### Infrastructure
- **Containerization:** Docker for all services (app, db, redis)
- **Orchestration:** Docker Compose for local, Kubernetes-ready for production
- **CI/CD:** GitHub Actions with automated testing, linting, and deployment
- **Monitoring:** Structured logging with Serilog/Logback, health check endpoints

---

## 2. Code Quality Standards

### General Principles
```
✅ DRY (Don't Repeat Yourself) — Extract reusable logic
✅ SOLID principles — Single responsibility, Open/closed, etc.
✅ Clean Code — Meaningful names, small functions, clear intent
✅ Type Safety — No `any` in TypeScript, proper generics usage
```

### Naming Conventions
| Layer | Convention | Example |
|-------|------------|---------|
| Components | PascalCase | `TransactionCard.tsx` |
| Hooks | camelCase with `use` prefix | `useTransactions.ts` |
| Services | camelCase | `transactionService.ts` |
| API Controllers | PascalCase + `Controller` suffix | `TransactionsController.cs` |
| Database Tables | snake_case | `transactions`, `user_accounts` |

### File Organization
```
/frontend
  /src
    /components      # Reusable UI components
    /features        # Feature-based modules (auth, transactions, etc.)
    /hooks           # Custom React hooks
    /services        # API service layer
    /types           # TypeScript interfaces/types
    /utils           # Utility functions
    /store           # State management

/backend
  /Controllers       # API endpoints
  /Services          # Business logic
  /DTOs              # Data transfer objects
  /Entities          # Database models
  /Repositories      # Data access layer
  /Middleware        # Custom middleware
```

---

## 3. Security Rules

### Authentication & Authorization
- All endpoints (except auth) require valid JWT
- Implement refresh token rotation
- Set token expiration: Access (15 min), Refresh (7 days)
- Rate limit login attempts (5 attempts per 15 minutes per IP)
- Store passwords with bcrypt (cost factor 12+) or Argon2id

### Data Protection
- All API communication over HTTPS only
- Sanitize all user inputs (prevent XSS/SQL injection)
- Use parameterized queries for all database operations
- Never expose sensitive data in logs or error messages
- Implement CORS with explicit allowed origins

### Financial Data Security
- Encrypt sensitive fields at rest if required by compliance
- Implement audit logging for all money-impacting operations
- Validate all financial calculations server-side
- Prevent negative amounts and overflow scenarios

---

## 4. API Design Standards

### Response Envelope
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed",
  "errors": null
}
```

### Error Handling
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": {
    "amount": ["Amount must be greater than 0"],
    "categoryId": ["Category is required"]
  }
}
```

### HTTP Status Codes
| Scenario | Status Code |
|----------|-------------|
| Success | 200 OK |
| Created | 201 Created |
| Validation Error | 400 Bad Request |
| Unauthorized | 401 Unauthorized |
| Forbidden | 403 Forbidden |
| Not Found | 404 Not Found |
| Server Error | 500 Internal Server Error |

### Pagination
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 150,
    "totalPages": 8
  }
}
```

---

## 5. Database Standards

### Schema Design
- Use UUID primary keys for all entities
- Foreign keys with proper cascade rules
- Timestamps on all tables (`created_at`, `updated_at`)
- Use appropriate data types (`numeric(12,2)` for money)
- Create indexes on frequently queried columns

### Query Optimization
- Use EF Core/LINQ efficiently (avoid N+1 queries)
- Implement database-level pagination
- Use `.AsNoTracking()` for read-only queries
- Profile slow queries and add indexes as needed

### Migration Strategy
- One migration per feature/change
- Never modify existing migrations after deployment
- Include seed data for default categories
- Test migrations on a copy of production data

---

## 6. Frontend Standards

### Component Structure
```tsx
// 1. Imports
import React from 'react';
import { useTransactions } from '@/hooks/useTransactions';

// 2. Types
interface TransactionCardProps {
  transaction: Transaction;
  onEdit: (id: string) => void;
}

// 3. Component
export const TransactionCard: React.FC<TransactionCardProps> = ({
  transaction,
  onEdit
}) => {
  // Logic here
  return (
    // JSX here
  );
};
```

### Performance Rules
- Use `React.memo` for expensive components
- Implement virtual scrolling for large lists
- Lazy load routes with `React.lazy()`
- Optimize images (WebP format, proper sizing)
- Debounce search inputs (300ms delay)

### Accessibility (a11y)
- All interactive elements keyboard accessible
- Proper ARIA labels for screen readers
- Color contrast ratio minimum 4.5:1
- Focus indicators visible
- Form labels associated with inputs

---

## 7. Testing Requirements

### Unit Tests
- **Frontend:** Jest + React Testing Library
- **Backend:** xUnit/NUnit for .NET, JUnit for Java
- Minimum 70% code coverage for business logic
- Test both success and error paths

### Integration Tests
- API endpoint testing with TestServer
- Database integration tests with in-memory/ test containers
- Authentication flow testing

### E2E Tests
- Playwright or Cypress for critical user flows
- Tests for: Login → Add Transaction → View Dashboard → Create Budget

### Test Naming
```
MethodName_StateUnderTest_ExpectedBehavior

Examples:
- AddTransaction_ValidData_ReturnsCreatedTransaction
- GetDashboard_UnauthorizedUser_Returns401
- Login_InvalidCredentials_ReturnsErrorMessage
```

---

## 8. Git Workflow

### Branch Strategy (GitFlow)
```
main        → Production-ready code
develop     → Integration branch
feature/*   → New features
bugfix/*    → Bug fixes
hotfix/*    → Emergency production fixes
release/*   → Release preparation
```

### Commit Message Convention
```
type(scope): subject

Types:
- feat: New feature
- fix: Bug fix
- docs: Documentation
- style: Formatting changes
- refactor: Code restructuring
- test: Adding tests
- chore: Build/tooling changes

Examples:
- feat(transactions): add bulk delete functionality
- fix(auth): resolve token refresh issue
- test(budgets): add unit tests for budget service
```

### Pull Request Rules
- Require 1+ code review approval
- All CI checks must pass
- No merge conflicts with target branch
- PR description includes: What, Why, How to test

---

## 9. Performance Standards

### Backend
- API response time < 200ms (p95)
- Database queries < 50ms
- Implement caching for dashboard data (Redis)
- Use async/await throughout

### Frontend
- First Contentful Paint < 1.5s
- Time to Interactive < 3s
- Bundle size < 200KB (gzipped)
- Implement code splitting by route

### Database
- Connection pooling enabled
- Query timeout: 30 seconds
- Regular VACUUM and ANALYZE for PostgreSQL

---

## 10. Documentation Standards

### Code Documentation
- XML comments for all public APIs (.NET)
- JSDoc for complex functions (TypeScript)
- README in each feature folder explaining purpose

### API Documentation
- Swagger UI at `/swagger`
- Include request/response examples
- Document error scenarios
- Authentication requirements clearly stated

### Project Documentation
- `README.md` with setup instructions
- `CONTRIBUTING.md` with development guidelines
- `CHANGELOG.md` tracking version changes
- Architecture Decision Records (ADRs) for major decisions

---

## 11. Deployment Rules

### Environment Strategy
| Environment | Purpose | Auto-deploy |
|-------------|---------|-------------|
| Local | Development | N/A |
| Staging | Testing/QA | From `develop` |
| Production | Live users | Manual trigger |

### Deployment Checklist
- [ ] All tests passing
- [ ] Database migrations reviewed
- [ ] Environment variables configured
- [ ] Health checks implemented
- [ ] Rollback plan documented

### Monitoring
- Application logs centralized
- Error tracking (Sentry or similar)
- Performance metrics (APM)
- Uptime monitoring

---

## 12. Definition of Done

A feature is **complete** when:
1. ✅ Code implemented following standards
2. ✅ Unit tests written and passing
3. ✅ Integration tests added
4. ✅ Code reviewed and approved
5. ✅ Documentation updated
6. ✅ No security vulnerabilities
7. ✅ Performance benchmarks met
8. ✅ Accessibility requirements met
9. ✅ Tested in staging environment
10. ✅ Product owner acceptance

---

*These rules ensure a high-quality, maintainable, and scalable Personal Finance Tracker that follows modern software engineering best practices.*
