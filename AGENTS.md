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

# 9. Domain Event 규칙

- Domain Layer에 정의
- 순수 데이터 구조
- 외부 프레임워크 의존 금지
- 발행/처리는 Application 또는 Infrastructure에서 수행

---

# 10. 예외 처리 규칙 (중요)

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

# 11. Handler 반환 규칙 (중요)

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

public Result<OrderResponse> Handle(CreateOrderCommand command)
{
    if (command.Price <= 0)
        return Result.Failure<OrderResponse>("Invalid price");

    var order = Order.Create(command.Id, command.Price);

    return Result.Success(new OrderResponse(order.Id));
}

---

# 12. DI 규칙

- 모든 구현체 등록은 Infrastructure에서 수행
- Application/Domain은 구현체를 모름
- 인터페이스에만 의존

# 13. 코딩 스타일 컨벤션

## 네이밍
- 배열: 복수형 ~s
- List: ~List

## var 규칙
- 기본적으로 타입을 명시한다
- 꼭 필요할 때만 var 허용

## CancellationToken 규칙
- 모든 외부 API 및 비동기 흐름에 포함

Task GetDataAsync(CancellationToken cancellationToken);

# 14. AI Agent 코드 생성 규칙

- Controller에 비즈니스 로직 금지
- 모든 유스케이스는 Handler로 구현
- Service 기반 유스케이스 금지
- Domain은 순수해야 함
- Infrastructure는 Application 인터페이스 구현
- Contracts는 Presentation에서 사용
- 의존성 방향 절대 위반 금지

# 15. 아키텍처 철학

- 모듈은 독립된 비즈니스 경계를 가진다
- CQRS는 기본 실행 모델이다
- Domain이 시스템의 유일한 진실이다
- Infrastructure는 교체 가능한 기술 계층이다
- Presentation은 API 계약과 검증 책임만 가진다