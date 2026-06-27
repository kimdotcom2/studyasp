using Microsoft.EntityFrameworkCore;      // EF Core (DbContext, DbSet<T> 등)
using studyasp.Models;                    // Board 모델 (테이블로 매핑할 클래스)

namespace studyasp.Data;

/// <summary>
/// 데이터베이스 연결과 테이블 목록을 관리하는 DbContext 클래스
/// 
/// - 이 클래스 하나가 DB 전체를 대표함
/// - DbSet<T> 속성 하나 = DB의 테이블 하나
/// - Program.cs에서 AddDbContext로 등록해야 사용 가능
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// 생성자 — DbContextOptions를 부모(DbContext)에게 전달
    /// </summary>
    /// <param name="options">
    /// Program.cs의 AddDbContext에서 전달받는 설정
    /// (어떤 DBMS를 쓸지, 연결 문자열은 무엇인지 등)
    /// </param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Boards 테이블
    /// 
    /// - DbSet<Board>  =  DB의 Boards 테이블
    /// - Boards.Add()  →  INSERT
    /// - Boards.Find() →  SELECT
    /// - Boards.Remove() → DELETE
    /// - await SaveChangesAsync()로 실제 DB에 반영
    /// </summary>
    public DbSet<Board> Boards => Set<Board>();

}