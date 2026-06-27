using System.ComponentModel.DataAnnotations;

namespace studyasp.Controllers.request;

public record PostBoardRequest(
    [Required] 
    [MinLength(1)]
    string Title,

    [Required] 
    [MinLength(1)]
    string Content
)
{
    
}