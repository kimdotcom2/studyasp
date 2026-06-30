# AGENTS.md (Modular Monolith + Clean Architecture + CQRS + Coding Convention)

이 프로젝트는 모듈러 모놀리스 구조이며, 각 모듈 내부는 Clean Architecture 원칙을 따른다.

# 1. 전체 아키텍처 원칙

- 각 모듈은 독립적인 Bounded Context이다.
- 모든 모듈은 내부적으로 Clean Architecture 구조를 가진다.
- 의존성 규칙은 항상 안쪽(Domain)으로 향한다.
- Application 계층은 CQRS(Command/Query + Handler) 기반으로 동작한다.
- Domain 계층은 순수 비즈니스 로직만 포함하며 외부 기술에 의존하지 않는다.

# 2. 모듈 구조 규칙

각 모듈은 반드시 다음 구조를 따른다:

/Modules/{ModuleName}
    /Domain
    /Application
    /Infrastructure
    /Presentation

## 각 계층 책임

### Domain
- Entity, ValueObject만 포함
- DomainEvent는 프로젝트에서 사용하지 않는다
- 비즈니스 규칙의 핵심
- 어떤 프레임워크에도 의존하지 않음

### Application
- Command / Query / Handler 포함
- DTO 및 인터페이스 정의
- 유스케이스 흐름 담당 (CQRS)

### Infrastructure
- EF Core, 외부 API, 메시지 브로커 구현
- Application 인터페이스 구현

### Presentation
- API 진입 계층
- Contracts(Request/Response), Validation 담당

# 3. Presentation 계층 규칙

/Presentation
    /Contracts
        /Requests
        /Responses
    /Dtos

- Request DTO: `{UseCase}Request`
- Response DTO: `{UseCase}Response`
- 일반 DTO: `{Name}Dto`
- Contracts는 Application DTO와 반드시 분리
- Controller는 MediatR의 `Send` 메서드를 통해 Command/Query를 Handler에 전달하며, 직접 비즈니스 로직을 수행하지 않는다

# 4. Validation 규칙 (Presentation)

- FluentValidation은 Presentation 계층에서만 사용
- Validation은 필요한 경우에만 적용 (필수 아님)
- `{Request}Validator`

# 5. Application 계층 규칙

- FluentValidation 사용 금지
- 모든 유스케이스는 Handler로 구현 (CQRS)
- 입력은 이미 검증된 상태라고 가정

## 공통 재사용 컴포넌트 (Application Service 개념)

- 여러 Handler에서 재사용되는 로직 존재 가능
- 개념적으로 Application Service에 해당하지만 Service라는 단어 사용 금지

### 네이밍
- Serializer
- Transformer
- Builder
- Module (애매한 경우)

예:
- OrderPriceCalculator
- OrderTransformer
- PaymentModule
- ShippingModule

### 금지
- Service 접미사 금지

# 6. CQRS 규칙

- Command: 상태 변경
- Query: 조회
- 각 Command/Query는 하나의 Handler

## 중재자 패턴 (Mediator Pattern)

- Controller(Presentation)는 MediatR의 `Send` 메서드로 Command/Query를 전송하고, 해당 Handler가 비즈니스 로직을 처리한다
- 이는 **중재자 패턴(Mediator Pattern)**에 해당하며, Presentation 계층과 Application 계층 간의 직접 의존성을 제거한다
- Controller는 Handler를 직접 호출하지 않고 MediatR을 통해서만 호출한다

### 호출 흐름

```text
Controller → MediatR.Send(Command/Query) → Handler → Domain Entity / Repository
```

# 7. Domain 규칙

## Entity 생성 규칙

- public parameterless constructor (EF Core용)
- private constructor (Domain 생성용)
- static factory method: Create

## 상태 변경 규칙

- 모든 setter는 private
- 상태 변경은 반드시 도메인 메서드로 수행

## 금지

- public setter 금지
- Entity를 DTO처럼 사용하는 것 금지

# 8. Infrastructure 규칙

- 기술 구현 계층
- Application 인터페이스 구현

## 네이밍

- Repository 제외 모든 구현체: `xxxService`

예:
- StripePaymentService
- SmtpEmailService
- RedisCacheService

# 9. DB 읽기/쓰기 분리 전략 (CQRS + N+1 방지) (중요)

## 기본 원칙

- **쓰기(CUD)**는 ORM(EF Core)을 통해 Domain Entity로 처리한다
- **읽기(R)**는 읽기 전용 DTO(`{Name}View`)를 통해 Dapper로 처리한다
- 같은 테이블이라도 쓰기와 읽기의 모델은 완전히 분리한다

## 읽기 전용 View DTO

### 위치

```
/Modules/{ModuleName}/Application/Dtos/{Name}View.cs
```

### 네이밍

- `{Name}View` (예: `BoardView`, `BoardDetailView`, `BoardListView`)

### 특징

- Application 계층에 정의한다
- 순수 데이터 구조 (로직 없음)
- Entity와 1:1 관계일 필요 없음 (Join, 집계 등 자유롭게 구성)
- public getter/setter 허용 (읽기 전용 단순 DTO이므로)
- 외부 프레임워크에 의존하지 않음

## Repository 구현 규칙

Repository는 **읽기용(Read)과 쓰기용(Write)으로 분리**한다.

- **Write Repository** — EF Core 기반, Domain Entity 처리
- **Read Repository** — Dapper 기반, 읽기 전용 View DTO 반환

모든 Repository의 **인터페이스는 Application 계층**에, **구현체는 Infrastructure 계층**에 두며, DI를 통해 주입한다.

### Write Repository

- `I{Entity}Repository` — Application/Interfaces 에 인터페이스 정의
- `{Entity}Repository` — Infrastructure/Persistence 에 EF Core로 구현
- `Task AddAsync(...)`, `Task UpdateAsync(...)`, `Task DeleteAsync(...)` 등 CUD 메서드
- Domain Entity를 인자로 받고, EF Core를 통해 DB에 반영
- 해당 모듈의 DbContext(`AppDbContext`)를 주입받아 사용

```csharp
// Application/Interfaces/IBoardRepository.cs
public interface IBoardRepository
{
    Task AddAsync(Board board, CancellationToken ct);
    Task UpdateAsync(Board board, CancellationToken ct);
    Task DeleteAsync(Board board, CancellationToken ct);
}

// Infrastructure/Persistence/BoardRepository.cs (EF Core 구현)
// AppDbContext를 주입받아 EF Core로 CUD 처리
public class BoardRepository : IBoardRepository
{
    private readonly AppDbContext _context;

    public BoardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Board board, CancellationToken ct)
    {
        await _context.Boards.AddAsync(board, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Board board, CancellationToken ct)
    {
        _context.Boards.Update(board);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Board board, CancellationToken ct)
    {
        _context.Boards.Remove(board);
        await _context.SaveChangesAsync(ct);
    }
}
```

### Read Repository

- `I{Entity}QueryRepository` — Application/Interfaces 에 인터페이스 정의
- `{Entity}QueryRepository` — Infrastructure/Persistence 에 Dapper로 구현
- `Task<{Name}View?> GetByIdAsync(...)`, `Task<List<{Name}View>> GetListAsync(...)` 등
- 읽기 전용 View DTO(`{Name}View`)만 반환

```csharp
// Application/Interfaces/IBoardQueryRepository.cs
public interface IBoardQueryRepository
{
    Task<BoardView?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<BoardListView>> GetListAsync(int page, int size, CancellationToken ct);
}

// Infrastructure/Persistence/BoardQueryRepository.cs (Dapper 구현)
public class BoardQueryRepository : IBoardQueryRepository
{
    private readonly string _connectionString;

    public BoardQueryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<BoardView?> GetByIdAsync(int id, CancellationToken ct)
    {
        using IDbConnection conn = new SqliteConnection(_connectionString);
        const string sql = @"
            SELECT b.Id, b.Title, b.Content, b.CreatedAt,
                   m.Name AS AuthorName,
                   COUNT(c.Id) AS CommentCount
            FROM Boards b
            LEFT JOIN Members m ON b.MemberId = m.Id
            LEFT JOIN Comments c ON c.BoardId = b.Id
            WHERE b.Id = @Id
            GROUP BY b.Id";
        return await conn.QuerySingleOrDefaultAsync<BoardView>(sql, new { Id = id });
    }

    public async Task<List<BoardListView>> GetListAsync(int page, int size, CancellationToken ct)
    {
        using IDbConnection conn = new SqliteConnection(_connectionString);
        const string sql = @"
            SELECT b.Id, b.Title, b.CreatedAt,
                   m.Name AS AuthorName,
                   COUNT(c.Id) AS CommentCount
            FROM Boards b
            LEFT JOIN Members m ON b.MemberId = m.Id
            LEFT JOIN Comments c ON c.BoardId = b.Id
            GROUP BY b.Id
            ORDER BY b.Id DESC
            LIMIT @Size OFFSET @Offset";
        return (await conn.QueryAsync<BoardListView>(sql, new
        {
            Size = size,
            Offset = (page - 1) * size
        })).ToList();
    }
}
```

### DI 등록

각 모듈은 **모듈 루트에 확장 메서드(Extension Method)** 파일을 두어 모듈 전체의 DI 설정을 분리한다. 모듈별로 하나의 확장 메서드 파일에서 해당 모듈의 **모든 계층의 서비스**(Application의 Transformer/Builder/Module, Infrastructure의 Repository, Presentation의 Validator, MediatR Handler 등)를 등록한다.

각 모듈은 **별개의 어셈블리(프로젝트)**로 분리될 수 있어야 하므로, MediatR Handler 등록 시 `RegisterServicesFromAssembly`에 **자신의 어셈블리**를 지정한다.

#### 규칙

- 파일 위치: `Modules/{ModuleName}/{ModuleName}DependencyInjection.cs`
- 메서드명: `Add{ModuleName}Module(this IServiceCollection services)`
- MediatR Handler 등록: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof({ModuleName}DependencyInjection).Assembly))`
- `Program.cs`에서는 각 모듈의 확장 메서드만 호출하면 된다

```csharp
// Modules/Board/BoardDependencyInjection.cs
public static class BoardDependencyInjection
{
    public static IServiceCollection AddBoardModule(this IServiceCollection services)
    {
        // MediatR Handler (자기 어셈블리 스캔)
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(BoardDependencyInjection).Assembly));

        // Write Repository (EF Core)
        services.AddScoped<IBoardRepository, BoardRepository>();

        // Read Repository (Dapper)
        services.AddScoped<IBoardQueryRepository>(provider =>
        {
            IConfiguration config = provider.GetRequiredService<IConfiguration>();
            return new BoardQueryRepository(
                config.GetConnectionString("DefaultConnection")!);
        });

        return services;
    }
}

// Modules/Member/MemberDependencyInjection.cs (다른 모듈 예시)
public static class MemberDependencyInjection
{
    public static IServiceCollection AddMemberModule(this IServiceCollection services)
    {
        // MediatR Handler (자기 어셈블리 스캔)
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MemberDependencyInjection).Assembly));

        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IMemberQueryRepository, MemberQueryRepository>();
        return services;
    }
}
```

#### Program.cs 사용 예시

```csharp
// Program.cs — 각 모듈의 확장 메서드만 호출
builder.Services.AddBoardModule();
builder.Services.AddMemberModule();
// 다른 모듈들...
```

이렇게 하면 `Program.cs`는 각 모듈의 내부 구성을 전혀 알 필요 없이 확장 메서드만 호출하면 되므로, 모듈 간 결합도가 낮아지고 응집도가 높아진다. 또한 각 모듈이 별도의 프로젝트(어셈블리)로 분리되어도 MediatR Handler가 정상 동작한다.

## N+1 문제 방지

### 문제 상황 (ORM으로 읽기할 때)

```csharp
// ❌ ORM으로 목록 조회 시 N+1 발생
List<Board> boards = await _context.Boards.ToListAsync();
foreach (Board board in boards)  // N번의 추가 쿼리 발생
{
    _context.Entry(board).Reference(b => b.Category).Load();
    _context.Entry(board).Collection(b => b.Comments).Load();
}
```

### 해결 (Dapper 읽기 전용 DTO)

```csharp
// ✅ Dapper로 Join 한 번에 해결
const string sql = @"
    SELECT b.Id, b.Title, b.Content, b.CreatedAt,
           c.Name AS CategoryName,
           (SELECT COUNT(*) FROM Comments WHERE BoardId = b.Id) AS CommentCount
    FROM Boards b
    LEFT JOIN Categories c ON b.CategoryId = c.Id
    ORDER BY b.CreatedAt DESC";

List<BoardListView> boards = await conn.QueryAsync<BoardListView>(sql);
```

## CQRS와의 연계

```csharp
// Query - Read Repository (Dapper)로 읽기 전용 View DTO 반환
public class GetBoardListHandler : IRequestHandler<GetBoardListQuery, Result<List<BoardListView>>>
{
    private readonly IBoardQueryRepository _queryRepo;  // Dapper

    public async Task<Result<List<BoardListView>>> Handle(
        GetBoardListQuery query, CancellationToken ct)
    {
        List<BoardListView> boards = await _queryRepo.GetListAsync(query.Page, query.Size, ct);
        return Result.Success(boards);
    }
}

// Command - Write Repository (EF Core)로 Domain Entity 저장
public class CreateBoardHandler : IRequestHandler<CreateBoardCommand, Result<BoardDto>>
{
    private readonly IBoardRepository _repo;  // EF Core

    public async Task<Result<BoardDto>> Handle(
        CreateBoardCommand command, CancellationToken ct)
    {
        Board board = Board.Create(command.Title, command.Content, command.MemberId);
        await _repo.AddAsync(board, ct);
        return Result.Success(new BoardDto(board.Id, board.Title));
    }
}
```

## 요약

| 구분 | 쓰기 (Command) | 읽기 (Query) |
|------|---------------|-------------|
| Repository | Write Repository (`IBoardRepository`) | Read Repository (`IBoardQueryRepository`) |
| 기술 | EF Core (ORM) | Dapper (ADO.NET) |
| 모델 | Domain Entity (`Board`) | 읽기 전용 View DTO (`BoardView`) |
| Interface 위치 | `Application/Interfaces/` | `Application/Interfaces/` |
| 구현 위치 | `Infrastructure/Persistence/` (EF Core) | `Infrastructure/Persistence/` (Dapper) |
| 반환 | void 또는 Entity Id | `{Name}View` 또는 `List<{Name}View>` |
| 특징 | 변경 감지, 트랜잭션 | Join, 집계, 프로젝션 자유로움 |
| N+1 | 발생 가능 (주의) | 발생하지 않음 (SQL 직접 제어) |

---

# 10. Domain Event 규칙

- Domain Layer에 정의
- 순수 데이터 구조
- 외부 프레임워크 의존 금지
- 발행/처리는 Application 또는 Infrastructure에서 수행

---

# 11. 예외 처리 규칙 (중요)

## 기본 원칙

- Domain/Application/Infrastructure에서는 예외를 throw한다
- Controller에서는 try-catch를 사용하지 않는다
- 모든 예외 처리는 ASP.NET Core Middleware에서 처리한다

## 계층별 책임

- Domain: DomainException throw
- Application: NotFoundException / BusinessException throw
- Infrastructure: 외부 예외를 그대로 throw 또는 wrapping
- Presentation: try-catch 금지, Middleware로 처리

## 표준 Exception 타입

- DomainException
- NotFoundException
- ValidationException
- UnauthorizedException
- ForbiddenException
- InfrastructureException

## HTTP 매핑 규칙

- 400: DomainException / ValidationException
- 401: UnauthorizedException
- 403: ForbiddenException
- 404: NotFoundException
- 500: 그 외 모든 예외

## Middleware 처리

- Global Exception Middleware에서 모든 예외 처리
- Logging 수행
- JSON response로 변환

---

# 12. Handler 반환 규칙 (중요)

## 기본 전략 (권장)

- 모든 Handler의 반환 타입은 기본적으로 `Result<T>` 또는 `Task<Result<T>>`여야 한다
- 비동기 Handler는 반드시 `Task<Result<T>>`를 사용한다
- 비즈니스 실패는 Exception이 아닌 Result로 표현한다
- 시스템 예외만 Exception으로 처리한다

## Result 패턴 의미

- 성공/실패를 값으로 표현
- 예외를 흐름 제어로 사용하지 않음

## 사용 기준

- Result: validation 실패, 비즈니스 규칙 실패, 예상 가능한 실패
- Exception: 시스템 오류, DB/외부 API 장애, 비정상 상황

## Handler 예시

```csharp
public Result<OrderResponse> Handle(CreateOrderCommand command)
{
    if (command.Price <= 0)
        return Result.Failure<OrderResponse>("Invalid price");

    Order order = Order.Create(command.Id, command.Price);

    return Result.Success(new OrderResponse(order.Id));
}
```

---

# 13. DI 규칙

- 모든 구현체 등록은 Infrastructure에서 수행
- Application/Domain은 구현체를 모름
- 인터페이스에만 의존

# 14. 코딩 스타일 컨벤션

## 네이밍
- 배열: 복수형 ~s
- List: ~List

## var 규칙
- 기본적으로 타입을 명시한다
- 꼭 필요할 때만 var 허용

## CancellationToken 규칙
- 모든 외부 API 및 비동기 흐름에 포함

Task GetDataAsync(CancellationToken cancellationToken);

# 15. AI Agent 코드 생성 규칙

- Controller에 비즈니스 로직 금지
- 모든 유스케이스는 Handler로 구현
- Service 기반 유스케이스 금지
- Domain은 순수해야 함
- Infrastructure는 Application 인터페이스 구현
- Contracts는 Presentation에서 사용
- 의존성 방향 절대 위반 금지

# 16. 아키텍처 철학

- 모듈은 독립된 비즈니스 경계를 가진다
- CQRS는 기본 실행 모델이다
- Domain이 시스템의 유일한 진실이다
- Infrastructure는 교체 가능한 기술 계층이다
- Presentation은 API 계약과 검증 책임만 가진다

---

# 17. 테스트 전략

## 중요도 순서

1. **통합 테스트 (외부 API/기술)** — 가장 중요
   - 외부 API, 데이터베이스, 파일 시스템 등 외부 기술과 상호작용하는 코드 검증
   - 실제 DB, 실제 HTTP 호출 등 환경 구성
2. **일반 통합 테스트** — 중요
   - 모듈 내 여러 계층이 함께 동작하는 흐름 검증
   - 예) Handler → Repository → DbContext 전체 흐름
3. **단위 테스트** — 기본
   - 순수 비즈니스 로직 검증 (Domain Entity, ValueObject 등)
   - 외부 의존성은 **Moq**을 사용하여 모킹

## 도구

- **Moq** — 단위 테스트에서 인터페이스 모킹
- **xUnit** 또는 **NUnit** — 테스트 러너
- **Testcontainers** — 통합 테스트용 실제 DB 컨테이너

---

# 18. 모듈 간 통신

## 원칙

- 모듈 간 직접 참조(클래스 호출) 금지
- 모든 모듈 간 통신은 **MediatR을 사용한 이벤트 드리븐** 방식으로 처리한다
- 이는 **옵저버 패턴(Observer Pattern)**에 해당하며, 발행자(Publisher)와 구독자(Subscriber)가 Notification을 매개로 느슨하게 결합된다

## 방식

- 한 모듈의 Command/Query Handler에서 필요한 경우 MediatR의 `IPublisher`를 통해 **Notification(이벤트)**을 발행
- 다른 모듈의 `INotificationHandler`가 해당 이벤트를 구독하여 처리

```csharp
// Modules/Board/Application/Events/BoardCreatedEvent.cs
// 모듈 간 통신용 Notification
public class BoardCreatedEvent : INotification
{
    public int BoardId { get; }
    public string Title { get; }
    public int MemberId { get; }

    public BoardCreatedEvent(int boardId, string title, int memberId)
    {
        BoardId = boardId;
        Title = title;
        MemberId = memberId;
    }
}

// Modules/Notification/Application/Handlers/SendBoardCreatedNotificationHandler.cs
// 다른 모듈에서 이벤트 구독
public class SendBoardCreatedNotificationHandler : INotificationHandler<BoardCreatedEvent>
{
    public async Task Handle(BoardCreatedEvent notification, CancellationToken ct)
    {
        // 게시글 작성 알림 발송 로직
    }
}
```

## 발행 흐름

```csharp
// Command Handler에서 이벤트 발행
public class CreateBoardHandler : IRequestHandler<CreateBoardCommand, Result<BoardDto>>
{
    private readonly IBoardRepository _repo;
    private readonly IPublisher _publisher;

    public CreateBoardHandler(IBoardRepository repo, IPublisher publisher)
    {
        _repo = repo;
        _publisher = publisher;
    }

    public async Task<Result<BoardDto>> Handle(CreateBoardCommand command, CancellationToken ct)
    {
        Board board = Board.Create(command.Title, command.Content, command.MemberId);
        await _repo.AddAsync(board, ct);

        // 이벤트 발행
        await _publisher.Publish(
            new BoardCreatedEvent(board.Id, board.Title, board.MemberId), ct);

        return Result.Success(new BoardDto(board.Id, board.Title));
    }
}
```

## 규칙

- 이벤트 클래스는 발행 모듈의 `Application/Events/`에 정의한다
- 이벤트 구독 Handler는 구독 모듈의 `Application/Handlers/`에 정의한다
- 이벤트는 순수 데이터 구조여야 하며, 비즈니스 로직을 포함하지 않는다
- 모듈 간 순환 참조가 발생하지 않도록 주의한다