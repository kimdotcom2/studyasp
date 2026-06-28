// ── Board 관련 DI 등록을 확장 메소드로 분리 ──────────────
// Program.cs에서 직접 AddScoped<BoardService>()를 호출하는 대신
// 이 확장 메소드를 호출하면 Board 서비스들의 DI 등록을 한 곳에서 관리할 수 있음
//
// 사용법 (Program.cs):
//   builder.Services.AddBoardService();
//
// 네임스페이스를 Microsoft.Extensions.DependencyInjection 으로 하면
// IServiceCollection에 직접 확장 메소드가 붙어 using 문 없이 사용 가능

namespace studyasp.Services;

/// <summary>
/// Board 도메인의 서비스들을 DI 컨테이너에 등록하기 위한 확장 메소드를 제공하는 클래스
/// </summary>
public static class BoardDependencyInjection
{
    /// <summary>
    /// BoardService를 DI 컨테이너에 Scoped 수명으로 등록합니다.
    /// </summary>
    /// <param name="services">DI 컨테이너 역할을 하는 IServiceCollection</param>
    /// <returns>메소드 체이닝을 위해 IServiceCollection을 그대로 반환</returns>
    public static IServiceCollection AddBoardService(this IServiceCollection services)
    {
        // Scoped: HTTP 요청 1회 동안 같은 인스턴스를 재사용
        services.AddScoped<BoardService>();
        return services;
    }
}