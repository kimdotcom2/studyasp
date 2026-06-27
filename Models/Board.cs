using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace studyasp.Models;

// 테이블과 매핑
[Table("Board")]
public class Board
{

    // DB에서 쿼리로 가져오기 EF가 리플렉션으로 전 빈 객체 생성하므로 필요
    private Board()
    {
    }

    [Key]                         // PK 명시 지정
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // 자동 증가
    public int Id { get; private set; }
    // 리플렉션 시 EF가 프로퍼티 값을 생성하기 위한 private set
    public string Title { get; private set; }
    public string Content { get; private set; }

    private Board(string title, string content)
    {
        Title = title;
        Content = content;
    }

    public static Board Of(string title, string content)
    {
        return new Board(title, content);
    }

    public void Update(string title, string content)
    {
        Title = title;
        Content = content;
    }

}