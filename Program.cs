// ── 1. 네임스페이스 불러오기 ──────────────────────────────
using Microsoft.EntityFrameworkCore;   // EF Core (UseSqlite, DbContext)
using Serilog;                           // Serilog (로그)
using studyasp.Data;
using studyasp.Repositories;
using studyasp.Services;                   // 우리가 만든 AppDbContext


// ── WebApplication 빌더 생성 ──────────────────────────────
// builder: 앱의 각종 설정(서비스, 미들웨어 등)을 등록하는 도구
var builder = WebApplication.CreateBuilder(args);

// ── Serilog 초기화 ────────────────────────────────────
// appsettings.json의 Serilog 설정을 읽어 로깅 구성
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// ── 서비스 등록 (Dependency Injection) ────────────────────

// MVC (Model-View-Controller) 방식 사용
builder.Services.AddControllersWithViews();

// ── Swagger / OpenAPI ──────────────────────────────────
// API 엔드포인트 자동 문서화 및 테스트 UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── DbContext (데이터베이스 연결) 등록 ─────────────────────
// AppDbContext : DB 연결 클래스 (Data/AppDbContext.cs)
// UseSqlite    : DBMS로 SQLite 사용 (파일 기반 DB)
// GetConnectionString("DefaultConnection") : appsettings.json에
//   설정한 연결 문자열을 읽어옴 ("Data Source=studyasp.db")
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))); 

// Service를 DI 컨테이너에 등록
builder.Services.AddBoardService();
builder.Services.AddScoped<BoardRepository>();

// MediatR 등록 (Command/Query Handler 자동 스캔)
builder.Services.AddMediatR(config => 
    config.RegisterServicesFromAssembly(typeof(Program).Assembly));

// ── 앱 빌드 (등록한 서비스들을 실제로 조립) ────────────────
var app = builder.Build();

// ── 미들웨어 파이프라인 (요청이 지나가는 관문들) ────────────

// HTTP 요청을 HTTPS로 자동 리다이렉트
app.UseHttpsRedirection();

// wwwroot 폴더의 정적 파일(HTML/CSS/JS/이미지) 제공
app.UseStaticFiles();

// URL 경로를 컨트롤러 액션에 매칭시키는 라우팅 활성화
app.UseRouting();

// ── Swagger UI (개발 환경에서만 활성화) ────────────────
app.UseSwagger();
app.UseSwaggerUI();

// ── 라우트 패턴 설정 (URL → 컨트롤러/액션 매핑) ────────────
// 예: /Board/Edit/5  →  BoardController.Edit(id=5)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
    //    └──────┬──────┘ └─────┬──────┘ └─┬─┘
    //   기본 컨트롤러: Home  기본 액션: Index  선택적 id
);

// ── 앱 실행 ──────────────────────────────────────────────
// Kestrel/IIS Express 서버가 뜨고 HTTP 요청을 수신 대기
app.Run();

// Serilog 로그 플러시 및 정리
Log.CloseAndFlush();
